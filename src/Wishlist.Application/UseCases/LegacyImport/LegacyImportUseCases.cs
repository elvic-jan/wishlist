using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.LegacyImport;

public static class LegacyImportRules
{
    /// <summary>Old items changed on or after this date are ticked by default.</summary>
    public static readonly DateTime SuggestedSince = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public const string NotFound = "We couldn't find that username.";
    public const string AlreadyClaimed = "This old account has already been imported.";
    public const string AlreadyImported = "You've already imported your wishes from the old site.";
    public const string TooManyTries = "Too many usernames tried. Please try again in an hour.";
}

public class GetLegacyImportStatusUseCase(ILegacyRepository legacy)
{
    public async Task<LegacyImportStatusDto> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var claim = await legacy.GetClaimedByAsync(userId, ct);
        if (claim is not null)
            return new LegacyImportStatusDto(true, true, claim.UserName, claim.ClaimedAt, claim.ImportedItemCount);

        return new LegacyImportStatusDto(await legacy.AnyAsync(ct), false, null, null, null);
    }
}

/// <summary>Shows an old username's live items. Claims nothing.</summary>
public class PreviewLegacyImportUseCase(ILegacyRepository legacy, ILegacyUserNameAttemptLimiter limiter)
{
    public async Task<LegacyPreviewDto> ExecuteAsync(Guid userId, string? userName, CancellationToken ct = default)
    {
        var owner = await LegacyImportGuard.ResolveAsync(legacy, limiter, userId, userName, ct);
        var items = await legacy.GetItemsAsync(owner.LegacyId, ct);

        return new LegacyPreviewDto(owner.UserName, items
            .OrderByDescending(i => i.ChangedAt ?? DateTime.MinValue)
            .ThenBy(i => i.LegacyId)
            .Select(i =>
            {
                var mapped = LegacyItemMapper.Map(i.Title, i.Description);
                return new LegacyItemPreviewDto(i.LegacyId, mapped.Title, mapped.Description, mapped.Urls, i.ChangedAt,
                    i.ChangedAt >= LegacyImportRules.SuggestedSince);
            })
            .Where(i => i.Title.Length > 0)
            .ToList());
    }
}

/// <summary>Imports the ticked old items of one old username, once per account and per username.</summary>
public class ImportLegacyItemsUseCase(
    ILegacyRepository legacy,
    ILegacyUserNameAttemptLimiter limiter,
    IWishlistRepository wishlists,
    IUnitOfWork unitOfWork,
    TimeProvider time)
{
    public async Task<LegacyImportStatusDto> ExecuteAsync(Guid userId, LegacyImportRequest request, CancellationToken ct = default)
    {
        var owner = await LegacyImportGuard.ResolveAsync(legacy, limiter, userId, request.UserName, ct);
        var wanted = (request.ItemIds ?? []).ToHashSet();
        var chosen = (await legacy.GetItemsAsync(owner.LegacyId, ct))
            .Where(i => wanted.Contains(i.LegacyId))
            .OrderBy(i => i.ChangedAt ?? DateTime.MinValue)
            .ThenBy(i => i.LegacyId)
            .Select(i => LegacyItemMapper.Map(i.Title, i.Description))
            .Where(m => m.Title.Length > 0)
            .ToList();

        var now = time.GetUtcNow().UtcDateTime;
        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            if (!await legacy.TryClaimAsync(owner.LegacyId, userId, now, chosen.Count, innerCt))
                throw new ConflictException(LegacyImportRules.AlreadyClaimed);

            foreach (var item in chosen)
                await wishlists.CreateAsync(userId, item.Title, item.Description, item.Urls, innerCt);
        }, ct);

        return new LegacyImportStatusDto(true, true, owner.UserName, now, chosen.Count);
    }
}

internal static class LegacyImportGuard
{
    /// <summary>The old username, if this account may import it; otherwise the matching error.</summary>
    public static async Task<LegacyUser> ResolveAsync(
        ILegacyRepository legacy, ILegacyUserNameAttemptLimiter limiter, Guid userId, string? userName, CancellationToken ct)
    {
        if (await legacy.GetClaimedByAsync(userId, ct) is not null)
            throw new ConflictException(LegacyImportRules.AlreadyImported);
        if (limiter.IsBlocked(userId))
            throw new TooManyRequestsException(LegacyImportRules.TooManyTries);

        var normalized = LegacyUser.Normalize(userName ?? string.Empty);
        var owner = normalized.Length == 0 ? null : await legacy.GetByUserNameAsync(normalized, ct);
        if (owner is null)
        {
            limiter.RecordFailure(userId);
            throw new NotFoundException(LegacyImportRules.NotFound);
        }

        if (owner.ClaimedByUserId is not null)
            throw new ConflictException(LegacyImportRules.AlreadyClaimed);

        return owner;
    }
}
