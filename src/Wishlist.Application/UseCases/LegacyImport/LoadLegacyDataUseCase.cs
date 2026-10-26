using System.Globalization;
using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;

namespace Wishlist.Application.UseCases.LegacyImport;

/// <summary>
/// Loads the old site's exported users and items spreadsheets. Keeps only
/// usernames and live items (not struck out, not deleted); never reads emails,
/// display names, passwords, priorities, remarks or reservation state.
/// </summary>
public class LoadLegacyDataUseCase(ISpreadsheetReader reader, ILegacyRepository legacy, ILogger<LoadLegacyDataUseCase> logger)
{
    private const string StruckOut = "3";

    public async Task<(int Users, int Items)> ExecuteAsync(string usersPath, string itemsPath, CancellationToken ct = default)
    {
        var users = reader.ReadRows(usersPath)
            .Select(r => (Id: Int(r, "USER_ID"), Name: Text(r, "NAME")))
            .Where(u => u.Id is not null && u.Name.Length > 0)
            .Select(u => new LegacyUser { LegacyId = u.Id!.Value, UserName = LegacyUser.Normalize(u.Name) })
            .ToList();
        var userIds = users.Select(u => u.LegacyId).ToHashSet();

        var items = new List<LegacyItem>();
        var skipped = 0;
        foreach (var r in reader.ReadRows(itemsPath))
        {
            var id = Int(r, "ITEM_ID");
            var owner = Int(r, "OWNER");
            var title = Text(r, "NAME");
            if (id is null || owner is null || !userIds.Contains(owner.Value) || title.Length == 0
                || Text(r, "STATE") == StruckOut || Text(r, "DELETED") == "1")
            {
                skipped++;
                continue;
            }

            items.Add(new LegacyItem
            {
                LegacyId = id.Value,
                LegacyOwnerId = owner.Value,
                Title = title.Length > LegacyItemMapper.MaxTitleLength ? title[..LegacyItemMapper.MaxTitleLength] : title,
                Description = Text(r, "DESCRIPTION") is { Length: > 0 } d ? d : null,
                ChangedAt = ExcelDate(Text(r, "STATE_TS")),
            });
        }

        var result = await legacy.UpsertAsync(users, items, ct);
        logger.LogInformation(
            "Legacy load: {Users} usernames and {Items} live items stored ({Skipped} items skipped as struck out, deleted or incomplete).",
            result.Users, result.Items, skipped);
        return result;
    }

    private static string Text(IReadOnlyDictionary<string, string> row, string column) =>
        row.TryGetValue(column, out var v) ? v.Trim() : string.Empty;

    private static int? Int(IReadOnlyDictionary<string, string> row, string column) =>
        int.TryParse(Text(row, column), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;

    /// <summary>Excel stores dates as days since 1899-12-30.</summary>
    private static DateTime? ExcelDate(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var days) && days > 0
            ? DateTime.SpecifyKind(new DateTime(1899, 12, 30).AddDays(days), DateTimeKind.Utc)
            : null;
}
