using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Application.Messages;
using Wishlist.Application.UseCases.LegacyImport;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Exceptions;
using Wishlist.Infrastructure.Services;

namespace Wishlist.API.Tests;

/// <summary>Importing wishes from the old site, and the messages that point people to it.</summary>
public class LegacyImportTests
{
    // ---- Spreadsheet reading and loading -------------------------------------------------

    [Fact]
    public void Xlsx_reader_maps_rows_by_header_with_shared_and_inline_strings()
    {
        var path = TestXlsx.Write(
            ["USER_ID", "NAME", "EMAILADDRESS"],
            [["21", "j", "x@example.test"], ["2", "JOHN.DOE", ""]]);

        var rows = new XlsxReader().ReadRows(path);

        Assert.Equal(2, rows.Count);
        Assert.Equal("21", rows[0]["USER_ID"]);
        Assert.Equal("j", rows[0]["NAME"]);
        Assert.Equal("", rows[1]["EMAILADDRESS"]);
    }

    [Fact]
    public async Task Load_keeps_usernames_and_live_items_only()
    {
        var users = TestXlsx.Write(["USER_ID", "NAME", "PASSWORD", "DISPLAYNAME", "EMAILADDRESS"],
            [["1", " john.doe ", "0123456789abcdef0123456789abcdef", "John", "john@example.test"], ["2", "JANE.DOE", "", "", ""]]);
        var items = TestXlsx.Write(["ITEM_ID", "OWNER", "NAME", "DESCRIPTION", "STATE", "STATE_TS", "DELETED", "PRIO", "REMARK"],
            [
                ["10", "1", "Bike", "Red one", "1", "45643.5", "", "1", "secret remark"],
                ["11", "1", "Struck", "", "3", "45643.5", "", "", ""],
                ["12", "1", "Deleted", "", "1", "45643.5", "1", "", ""],
                ["13", "2", "Book", "", "2", "", "", "", ""],
                ["14", "99", "Orphan", "", "1", "", "", "", ""],
            ]);
        var repo = new FakeLegacy();

        await new LoadLegacyDataUseCase(new XlsxReader(), repo, NullLogger<LoadLegacyDataUseCase>.Instance).ExecuteAsync(users, items);

        Assert.Equal(["JANE.DOE", "JOHN.DOE"], repo.Users.Select(u => u.UserName).Order());
        Assert.Equal([10, 13], repo.Items.Select(i => i.LegacyId).Order());
        var bike = repo.Items.Single(i => i.LegacyId == 10);
        Assert.Equal(("Bike", "Red one"), (bike.Title, bike.Description));
        Assert.Equal(new DateTime(2024, 12, 17, 12, 0, 0, DateTimeKind.Utc), bike.ChangedAt);
    }

    // ---- Mapping -------------------------------------------------------------------------

    [Fact]
    public void Mapping_strips_html_and_moves_links_out_of_the_description()
    {
        var m = LegacyItemMapper.Map(" Meringue ",
            "Van de <b>meringue sisters</b> &amp; zo,<br>zie https://example.test/shop?x=1. en www.example.org!\r\nGraag wit");

        Assert.Equal("Meringue", m.Title);
        Assert.Equal(["https://example.test/shop?x=1", "https://www.example.org"], m.Urls);
        Assert.Equal("Van de meringue sisters & zo,\nzie en\nGraag wit", m.Description);
    }

    [Fact]
    public void Mapping_keeps_at_most_ten_distinct_links_and_no_empty_description()
    {
        var links = string.Join(" ", Enumerable.Range(1, 12).Select(i => $"https://example.test/{i}"));
        var m = LegacyItemMapper.Map("x", links + " https://example.test/1");

        Assert.Equal(10, m.Urls.Count);
        Assert.Null(m.Description);
    }

    // ---- Preview and import --------------------------------------------------------------

    [Fact]
    public async Task Preview_matches_usernames_without_regard_to_case_and_suggests_recent_items()
    {
        var w = new World();
        w.Legacy.AddUser(5, "JOHN.DOE", (50, "Old", new DateTime(2015, 1, 1)), (51, "New", new DateTime(2025, 3, 1)));

        var preview = await w.Preview(" john.doe ");

        Assert.Equal("JOHN.DOE", preview.UserName);
        Assert.Equal([(51, true), (50, false)], preview.Items.Select(i => (i.LegacyId, i.Suggested)));
        Assert.Null(w.Legacy.Users.Single().ClaimedByUserId);
    }

    [Fact]
    public async Task Preview_of_an_unknown_username_is_404_and_counts_toward_the_limit()
    {
        var w = new World();
        w.Legacy.AddUser(5, "JOHN.DOE");

        for (var i = 0; i < LegacyUserNameAttemptLimiter.FailuresPerWindow; i++)
        {
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => w.Preview($"nobody{i}"));
            Assert.Equal(LegacyImportRules.NotFound, ex.Message);
        }

        // Blocked now, even for a real username.
        await Assert.ThrowsAsync<TooManyRequestsException>(() => w.Preview("JOHN.DOE"));
    }

    [Fact]
    public async Task Import_adds_only_ticked_items_of_that_username_and_claims_it()
    {
        var w = new World();
        w.Legacy.AddUser(5, "JOHN.DOE", (50, "Bike", null), (51, "Book", null));
        w.Legacy.AddUser(6, "JANE.DOE", (60, "Not mine", null));
        w.Legacy.Items.Single(i => i.LegacyId == 51).Description = "https://example.test/b";

        var status = await w.Import("John.Doe", 51, 60);

        Assert.Equal((true, "JOHN.DOE", 1), (status.Imported, status.UserName, status.ItemCount));
        var created = Assert.Single(w.Wishlists.Created);
        Assert.Equal(("Book", (string?)null, "https://example.test/b"), (created.Title, created.Description, created.Urls.Single()));
        Assert.Equal(w.UserId, w.Legacy.Users.Single(u => u.LegacyId == 5).ClaimedByUserId);
    }

    [Fact]
    public async Task Import_happens_once_per_account_and_once_per_username()
    {
        var w = new World();
        w.Legacy.AddUser(5, "JOHN.DOE", (50, "Bike", null));
        w.Legacy.AddUser(6, "JANE.DOE", (60, "Book", null));
        await w.Import("JOHN.DOE", 50);

        var again = await Assert.ThrowsAsync<ConflictException>(() => w.Import("JANE.DOE", 60));
        Assert.Equal(LegacyImportRules.AlreadyImported, again.Message);

        var other = new World(w.Legacy);
        var taken = await Assert.ThrowsAsync<ConflictException>(() => other.Import("JOHN.DOE", 50));
        Assert.Equal(LegacyImportRules.AlreadyClaimed, taken.Message);
        Assert.Single(w.Wishlists.Created);
        Assert.Empty(other.Wishlists.Created);
    }

    [Fact]
    public async Task Status_reports_nothing_loaded_then_the_import()
    {
        var w = new World();
        Assert.Equal(new LegacyImportStatusDto(false, false, null, null, null), await w.Status());

        w.Legacy.AddUser(5, "JOHN.DOE", (50, "Bike", null));
        Assert.Equal((true, false), ((await w.Status()).Available, (await w.Status()).Imported));

        await w.Import("JOHN.DOE", 50);
        var status = await w.Status();
        Assert.Equal((true, "JOHN.DOE", 1), (status.Imported, status.UserName, status.ItemCount));
    }

    // ---- Messages ------------------------------------------------------------------------

    [Fact]
    public async Task Import_message_shows_once_old_data_is_loaded_and_until_imported()
    {
        var w = new World();
        Assert.DoesNotContain(await w.Messages(), m => m.Id == "legacy-import");

        w.Legacy.AddUser(5, "JOHN.DOE", (50, "Bike", null));
        var message = Assert.Single(await w.Messages(), m => m.Id == "legacy-import");
        Assert.Equal(new MessageAction("Import my wishes", "/import"), message.Action);

        await w.Import("JOHN.DOE", 50);
        Assert.DoesNotContain(await w.Messages(), m => m.Id == "legacy-import");
    }

    [Fact]
    public async Task Dismissed_messages_stay_away_and_dismissing_is_idempotent()
    {
        var w = new World();
        Assert.Contains(await w.Messages(), m => m.Id == "whats-new-2026-11");

        await w.Dismiss("whats-new-2026-11");
        await w.Dismiss("whats-new-2026-11");

        Assert.DoesNotContain(await w.Messages(), m => m.Id == "whats-new-2026-11");
        Assert.Single(w.States.Dismissed);
    }

    [Fact]
    public async Task Messages_respect_their_date_window()
    {
        var w = new World { Now = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc) };

        Assert.DoesNotContain(await w.Messages(), m => m.Id == "whats-new-2026-11");
    }

    [Fact]
    public async Task Dismissing_an_unknown_message_is_404()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => new World().Dismiss("nope"));
    }

    // ---- Test world ----------------------------------------------------------------------

    private sealed class World(FakeLegacy? legacy = null)
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public FakeLegacy Legacy { get; } = legacy ?? new FakeLegacy();
        public FakeWishlists Wishlists { get; } = new();
        public FakeStates States { get; } = new();
        public DateTime Now { get; set; } = new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
        private readonly LegacyUserNameAttemptLimiter _limiter = new();

        private TimeProvider Clock => new FixedClock(Now);

        public Task<LegacyPreviewDto> Preview(string userName) =>
            new PreviewLegacyImportUseCase(Legacy, _limiter).ExecuteAsync(UserId, userName);

        public Task<LegacyImportStatusDto> Import(string userName, params int[] ids) =>
            new ImportLegacyItemsUseCase(Legacy, _limiter, Wishlists, new NoTransaction(), Clock)
                .ExecuteAsync(UserId, new LegacyImportRequest(userName, ids));

        public Task<LegacyImportStatusDto> Status() => new GetLegacyImportStatusUseCase(Legacy).ExecuteAsync(UserId);

        private IMessageDefinition[] Definitions => [new LegacyImportMessage(Legacy), new WhatsNewNovember2026Message()];

        public Task<IReadOnlyList<MessageDto>> Messages() => new GetMyMessagesUseCase(Definitions, States, Clock).ExecuteAsync(UserId);

        public Task Dismiss(string id) => new DismissMessageUseCase(Definitions, States, Clock).ExecuteAsync(UserId, id);
    }

    private sealed class FixedClock(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
    }

    private sealed class NoTransaction : IUnitOfWork
    {
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct = default) => work(ct);
    }

    private sealed class FakeLegacy : ILegacyRepository
    {
        public List<LegacyUser> Users { get; } = [];
        public List<LegacyItem> Items { get; } = [];

        public void AddUser(int id, string name, params (int Id, string Title, DateTime? ChangedAt)[] items)
        {
            Users.Add(new LegacyUser { LegacyId = id, UserName = name });
            Items.AddRange(items.Select(i => new LegacyItem
            {
                LegacyId = i.Id, LegacyOwnerId = id, Title = i.Title,
                ChangedAt = i.ChangedAt is { } d ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : null,
            }));
        }

        public Task<bool> AnyAsync(CancellationToken ct = default) => Task.FromResult(Users.Count > 0);
        public Task<LegacyUser?> GetByUserNameAsync(string userName, CancellationToken ct = default) => Task.FromResult(Users.FirstOrDefault(u => u.UserName == userName));
        public Task<LegacyUser?> GetClaimedByAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(Users.FirstOrDefault(u => u.ClaimedByUserId == userId));
        public Task<IReadOnlyList<LegacyItem>> GetItemsAsync(int legacyOwnerId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<LegacyItem>>(Items.Where(i => i.LegacyOwnerId == legacyOwnerId).ToList());

        public Task<bool> TryClaimAsync(int legacyId, Guid userId, DateTime claimedAt, int count, CancellationToken ct = default)
        {
            if (Users.Any(u => u.ClaimedByUserId == userId)) throw new ConflictException(LegacyImportRules.AlreadyImported);
            var user = Users.Single(u => u.LegacyId == legacyId);
            if (user.ClaimedByUserId is not null) return Task.FromResult(false);
            (user.ClaimedByUserId, user.ClaimedAt, user.ImportedItemCount) = (userId, claimedAt, count);
            return Task.FromResult(true);
        }

        public Task<(int Users, int Items)> UpsertAsync(IReadOnlyList<LegacyUser> users, IReadOnlyList<LegacyItem> items, CancellationToken ct = default)
        {
            Users.AddRange(users);
            Items.AddRange(items);
            return Task.FromResult((Users.Count, Items.Count));
        }
    }

    private sealed class FakeWishlists : IWishlistRepository
    {
        public List<(string Title, string? Description, IReadOnlyList<string> Urls)> Created { get; } = [];

        public Task<WishlistItem> CreateAsync(Guid ownerId, string title, string? description, IReadOnlyList<string> urls, CancellationToken ct = default)
        {
            Created.Add((title, description, urls));
            return Task.FromResult(new WishlistItem { Id = Guid.NewGuid(), OwnerId = ownerId, Title = title, Description = description });
        }

        public Task<IReadOnlyList<WishlistItem>> GetByOwnerAsync(Guid ownerId, bool includeClaims, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<WishlistItem?> GetByIdAsync(Guid itemId, bool includeClaim, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(WishlistItem item, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ReplaceUrlsAsync(WishlistItem item, IReadOnlyList<string> urls, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(Guid itemId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetGroupMemberSummariesAsync(Guid groupId, Guid excludeUserId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeStates : IUserMessageStateRepository
    {
        public HashSet<(Guid, string)> Dismissed { get; } = [];

        public Task<IReadOnlySet<string>> GetDismissedAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlySet<string>>(Dismissed.Where(d => d.Item1 == userId).Select(d => d.Item2).ToHashSet());

        public Task DismissAsync(Guid userId, string messageId, DateTime at, CancellationToken ct = default)
        {
            Dismissed.Add((userId, messageId));
            return Task.CompletedTask;
        }
    }
}

/// <summary>Writes a minimal one-sheet .xlsx (shared strings for the header, inline strings for data).</summary>
internal static class TestXlsx
{
    public static string Write(string[] header, string[][] rows)
    {
        var path = Path.Combine(Path.GetTempPath(), $"legacy-{Guid.NewGuid():N}.xlsx");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        void Add(string name, string content)
        {
            using var w = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false));
            w.Write(content);
        }

        const string ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        static string Esc(string s) => System.Security.SecurityElement.Escape(s);
        static string Col(int i) => ((char)('A' + i)).ToString();

        Add("xl/workbook.xml", $"""<workbook xmlns="{ns}" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="s" sheetId="1" r:id="rId1"/></sheets></workbook>""");
        Add("xl/_rels/workbook.xml.rels", """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="worksheet" Target="worksheets/sheet1.xml"/></Relationships>""");
        Add("xl/sharedStrings.xml", $"""<sst xmlns="{ns}">{string.Concat(header.Select(h => $"<si><t>{Esc(h)}</t></si>"))}</sst>""");

        var sb = new StringBuilder($"""<worksheet xmlns="{ns}"><sheetData><row r="1">""");
        for (var i = 0; i < header.Length; i++) sb.Append($"""<c r="{Col(i)}1" t="s"><v>{i}</v></c>""");
        sb.Append("</row>");
        for (var r = 0; r < rows.Length; r++)
        {
            sb.Append($"""<row r="{r + 2}">""");
            for (var c = 0; c < rows[r].Length; c++)
                if (rows[r][c].Length > 0)
                    sb.Append($"""<c r="{Col(c)}{r + 2}" t="inlineStr"><is><t xml:space="preserve">{Esc(rows[r][c])}</t></is></c>""");
            sb.Append("</row>");
        }
        sb.Append("</sheetData></worksheet>");
        Add("xl/worksheets/sheet1.xml", sb.ToString());
        return path;
    }
}
