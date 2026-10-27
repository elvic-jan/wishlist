using Microsoft.Extensions.Logging.Abstractions;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.GiftClaims;
using Wishlist.Application.UseCases.Wishlists;
using Wishlist.Domain;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.API.Tests;

/// <summary>Reservations end after 6 months (on a monthly run), with a reminder a month before.</summary>
public class ReservationExpiryTests
{
    private static DateTime Utc(int y, int m, int d, int h = 0) => new(y, m, d, h, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(2027, 1, 2, 10, 2027, 8, 1)]  // 2 Jul + rounding up
    [InlineData(2027, 1, 31, 0, 2027, 8, 1)]  // 31 Jul + rounding up
    [InlineData(2027, 1, 1, 7, 2027, 7, 1)]   // lands exactly on a run
    [InlineData(2027, 1, 1, 8, 2027, 8, 1)]   // just after a run
    [InlineData(2026, 8, 31, 12, 2027, 3, 1)] // 28 Feb + rounding up
    public void End_date_is_six_months_rounded_up_to_the_next_run(int y, int m, int d, int h, int ey, int em, int ed)
    {
        var end = ReservationSchedule.EndDateFrom(Utc(y, m, d, h));

        Assert.Equal(Utc(ey, em, ed, ReservationSchedule.RunHourUtc), end);
        Assert.Equal(DateTimeKind.Utc, end.Kind);
        Assert.Equal(Utc(ey, em, ed, 7).AddMonths(-1), ReservationSchedule.ReminderDueFor(end));
    }

    [Fact]
    public async Task Reserving_sets_the_end_date()
    {
        var w = new World(Utc(2027, 1, 2, 10));
        var item = w.AddItem(w.Owner);

        var dto = await w.Reserve(w.Giver, item);

        Assert.Equal(Utc(2027, 8, 1, 7), item.GiftClaim!.ExpiresAt);
        Assert.Equal(Utc(2027, 8, 1, 7), dto.Claim!.ExpiresAt);
    }

    [Fact]
    public async Task An_expired_reservation_shows_as_free_and_can_be_reserved_again()
    {
        var w = new World(Utc(2027, 1, 2, 10));
        var item = w.AddItem(w.Owner);
        await w.Reserve(w.Giver, item);

        w.Clock.Now = Utc(2027, 8, 1, 7);
        var seen = await w.GetWishlist(w.OtherGiver, w.Owner);
        Assert.Null(seen.Single().Claim);

        var dto = await w.Reserve(w.OtherGiver, item);
        Assert.Equal(w.OtherGiver.Id, item.GiftClaim!.ClaimedByUserId);
        Assert.True(dto.Claim!.IsClaimedByMe);
        Assert.Equal(Utc(2028, 2, 1, 7), item.GiftClaim.ExpiresAt);
    }

    [Fact]
    public async Task A_running_reservation_still_blocks_others()
    {
        var w = new World(Utc(2027, 1, 2, 10));
        var item = w.AddItem(w.Owner);
        await w.Reserve(w.Giver, item);

        w.Clock.Now = Utc(2027, 8, 1, 6);
        await Assert.ThrowsAsync<ConflictException>(() => w.Reserve(w.OtherGiver, item));
    }

    [Fact]
    public async Task Only_the_reserver_sees_the_end_date()
    {
        var w = new World(Utc(2027, 1, 2, 10));
        var item = w.AddItem(w.Owner);
        await w.Reserve(w.Giver, item);

        var mine = (await w.GetWishlist(w.Giver, w.Owner)).Single().Claim!;
        var theirs = (await w.GetWishlist(w.OtherGiver, w.Owner)).Single().Claim!;

        Assert.Equal(Utc(2027, 8, 1, 7), mine.ExpiresAt);
        Assert.Null(theirs.ExpiresAt);
        Assert.Equal(GiftClaimStatus.Reserved, theirs.Status);
    }

    [Fact]
    public async Task Keeping_gives_a_new_end_date_and_a_new_reminder()
    {
        var w = new World(Utc(2027, 1, 2, 10));
        var item = w.AddItem(w.Owner);
        await w.Reserve(w.Giver, item);
        item.GiftClaim!.ReminderSentAt = Utc(2027, 7, 1, 7);

        w.Clock.Now = Utc(2027, 7, 15, 9);
        var dto = await w.Keep(w.Giver, item);

        Assert.Equal(Utc(2028, 2, 1, 7), item.GiftClaim.ExpiresAt);
        Assert.Null(item.GiftClaim.ReminderSentAt);
        Assert.Equal(Utc(2028, 2, 1, 7), dto.Claim!.ExpiresAt);
    }

    [Fact]
    public async Task Only_the_reserver_can_keep_a_reservation()
    {
        var w = new World(Utc(2027, 1, 2, 10));
        var item = w.AddItem(w.Owner);
        await w.Reserve(w.Giver, item);

        await Assert.ThrowsAsync<ForbiddenException>(() => w.Keep(w.OtherGiver, item));
    }

    [Fact]
    public async Task Expired_or_bought_claims_cant_be_kept()
    {
        var w = new World(Utc(2027, 1, 2, 10));
        var expired = w.AddItem(w.Owner);
        var bought = w.AddItem(w.Owner);
        await w.Reserve(w.Giver, expired);
        await w.Reserve(w.Giver, bought);
        await w.MarkBought(w.Giver, bought);

        w.Clock.Now = Utc(2027, 8, 2);
        await Assert.ThrowsAsync<ConflictException>(() => w.Keep(w.Giver, expired));
        await Assert.ThrowsAsync<ConflictException>(() => w.Keep(w.Giver, bought));
    }

    [Fact]
    public async Task Bought_never_expires()
    {
        var w = new World(Utc(2027, 1, 2, 10));
        var item = w.AddItem(w.Owner);
        await w.Reserve(w.Giver, item);
        await w.MarkBought(w.Giver, item);

        Assert.Null(item.GiftClaim!.ExpiresAt);
        w.Clock.Now = Utc(2040, 1, 1);
        var seen = (await w.GetWishlist(w.OtherGiver, w.Owner)).Single().Claim;
        Assert.Equal(GiftClaimStatus.Bought, seen!.Status);

        var run = await w.RunMonthlyJob();
        Assert.Equal(0, run.Expired);
        Assert.NotNull(item.GiftClaim);
    }

    [Fact]
    public async Task Monthly_run_reminds_the_reserver_once_a_month_before_the_end()
    {
        var w = new World(Utc(2027, 1, 2, 10));
        var item = w.AddItem(w.Owner, "Red bike");
        await w.Reserve(w.Giver, item);

        // 1 June: the reservation ends 1 August, so no reminder yet.
        w.Clock.Now = Utc(2027, 6, 1, 7);
        Assert.Equal(0, (await w.RunMonthlyJob()).RemindersSent);

        // 1 July (a few seconds late): reminder.
        w.Clock.Now = Utc(2027, 7, 1, 7).AddSeconds(20);
        Assert.Equal(1, (await w.RunMonthlyJob()).RemindersSent);
        var email = Assert.Single(w.Emails);
        Assert.Equal(w.Giver.Email, email.To);
        Assert.Contains("Red bike", email.Subject);
        Assert.Contains("1 August 2027", email.Subject);
        Assert.Contains($"https://giftcircles.test/wishlist/{w.Owner.Id}", email.PlainTextBody);
        Assert.Contains("keep your reservation", email.PlainTextBody);
        Assert.NotNull(item.GiftClaim!.ReminderSentAt);

        // Running again (e.g. a retry) sends nothing new.
        Assert.Equal(0, (await w.RunMonthlyJob()).RemindersSent);
        Assert.Single(w.Emails);
    }

    [Fact]
    public async Task No_reminder_for_bought_or_released_gifts()
    {
        var w = new World(Utc(2027, 1, 2, 10));
        var bought = w.AddItem(w.Owner);
        var released = w.AddItem(w.Owner);
        await w.Reserve(w.Giver, bought);
        await w.Reserve(w.Giver, released);
        await w.MarkBought(w.Giver, bought);
        await w.Release(w.Giver, released);

        w.Clock.Now = Utc(2027, 7, 1, 7);
        await w.RunMonthlyJob();

        Assert.Empty(w.Emails);
    }

    [Fact]
    public async Task Monthly_run_ends_expired_reservations_and_tells_the_reserver()
    {
        var w = new World(Utc(2027, 1, 2, 10));
        var item = w.AddItem(w.Owner, "Red bike");
        await w.Reserve(w.Giver, item);

        w.Clock.Now = Utc(2027, 8, 1, 7);
        var run = await w.RunMonthlyJob();

        Assert.Equal(1, run.Expired);
        Assert.Null(item.GiftClaim);
        var email = Assert.Single(w.Emails);
        Assert.Equal(w.Giver.Email, email.To);
        Assert.Contains("has ended", email.Subject);
        Assert.Contains("Red bike", email.PlainTextBody);
        Assert.DoesNotContain(w.Emails, e => e.To == w.Owner.Email);
    }

    [Fact]
    public async Task A_failed_email_doesnt_keep_the_item_blocked()
    {
        var w = new World(Utc(2027, 1, 2, 10), failEmails: true);
        var item = w.AddItem(w.Owner);
        await w.Reserve(w.Giver, item);

        // Reminder fails: not marked sent, so it's tried again later.
        w.Clock.Now = Utc(2027, 7, 1, 7);
        var reminder = await w.RunMonthlyJob();
        Assert.Equal((0, 1), (reminder.RemindersSent, reminder.RemindersFailed));
        Assert.Null(item.GiftClaim!.ReminderSentAt);

        // Expiry email fails: the reservation ends anyway.
        w.Clock.Now = Utc(2027, 8, 1, 7);
        var expiry = await w.RunMonthlyJob();
        Assert.Equal((1, 0), (expiry.Expired, expiry.ExpiredEmailed));
        Assert.Null(item.GiftClaim);
    }

    /// <summary>Owner, two givers in one circle, items and claims in memory, and a settable clock.</summary>
    private sealed class World
    {
        private readonly InMemoryStore _store = new();
        private readonly Items _items = new();
        private readonly Claims _claims;
        private readonly InMemoryUsers _users;
        private readonly ReservationEmails _reservationEmails;

        public World(DateTime now, bool failEmails = false)
        {
            Clock = new Clock { Now = now };
            _claims = new Claims(_items);
            _users = new InMemoryUsers(_store);
            Owner = _store.AddUser("owner@example.test");
            Giver = _store.AddUser("giver@example.test");
            OtherGiver = _store.AddUser("other@example.test");
            IEmailSender sender = failEmails ? new FailingSender() : new CapturingEmailSender(_store);
            _reservationEmails = new ReservationEmails(sender, new Urls(), NullLogger<ReservationEmails>.Instance);
        }

        public Clock Clock { get; }
        public User Owner { get; }
        public User Giver { get; }
        public User OtherGiver { get; }
        public IReadOnlyList<EmailMessage> Emails => _store.SentEmails.ToList();

        public WishlistItem AddItem(User owner, string title = "Gift")
        {
            var item = new WishlistItem { Id = Guid.NewGuid(), OwnerId = owner.Id, Title = title, CreatedAt = Clock.Now };
            _items.All.Add(item);
            return item;
        }

        public Task<Application.DTOs.WishlistItemDto> Reserve(User who, WishlistItem item) =>
            new ReserveGiftUseCase(_items, _claims, new SharedCircle(), Clock).ExecuteAsync(who.Id, item.OwnerId, item.Id);

        public Task<Application.DTOs.WishlistItemDto> Keep(User who, WishlistItem item) =>
            new KeepReservationUseCase(_items, _claims, Clock).ExecuteAsync(who.Id, item.OwnerId, item.Id);

        public Task<Application.DTOs.WishlistItemDto> MarkBought(User who, WishlistItem item) =>
            new MarkGiftAsBoughtUseCase(_items, _claims, Clock).ExecuteAsync(who.Id, item.OwnerId, item.Id);

        public Task Release(User who, WishlistItem item) =>
            new ReleaseGiftClaimUseCase(_items, _claims, Clock).ExecuteAsync(who.Id, item.OwnerId, item.Id);

        public Task<IReadOnlyList<Application.DTOs.WishlistItemDto>> GetWishlist(User viewer, User owner) =>
            new GetMemberWishlistUseCase(_items, new SharedCircle(), Clock).ExecuteAsync(viewer.Id, owner.Id);

        public Task<ReservationRunResult> RunMonthlyJob() =>
            new ProcessReservationsUseCase(_claims, _users, _reservationEmails, Clock,
                NullLogger<ProcessReservationsUseCase>.Instance).ExecuteAsync();
    }

    private sealed class Clock : TimeProvider
    {
        public DateTime Now { get; set; }
        public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
    }

    private sealed class Urls : IAppUrlProvider
    {
        public string PublicBaseUrl => "https://giftcircles.test/";
    }

    private sealed class FailingSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken ct = default) =>
            throw new InvalidOperationException("Email service down.");
    }

    private sealed class Items : IWishlistRepository
    {
        public List<WishlistItem> All { get; } = [];

        public Task<IReadOnlyList<WishlistItem>> GetByOwnerAsync(Guid ownerId, bool includeClaims, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WishlistItem>>(All.Where(i => i.OwnerId == ownerId).ToList());

        public Task<WishlistItem?> GetByIdAsync(Guid itemId, bool includeClaim, CancellationToken ct = default) =>
            Task.FromResult(All.FirstOrDefault(i => i.Id == itemId));

        public Task<WishlistItem> CreateAsync(Guid ownerId, string title, string? description, IReadOnlyList<string> urls, CancellationToken ct = default, Guid? addedById = null) => throw new NotSupportedException();
        public Task UpdateAsync(WishlistItem item, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ReplaceUrlsAsync(WishlistItem item, IReadOnlyList<string> urls, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(Guid itemId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetGroupMemberSummariesAsync(Guid groupId, Guid excludeUserId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    /// <summary>Claims live on their items, like the one-claim-per-item table.</summary>
    private sealed class Claims(Items items) : IGiftClaimRepository
    {
        private IEnumerable<(WishlistItem Item, GiftClaim Claim)> All =>
            items.All.Where(i => i.GiftClaim is not null).Select(i => (i, i.GiftClaim!));

        public Task<GiftClaim?> GetByItemIdAsync(Guid wishlistItemId, CancellationToken ct = default) =>
            Task.FromResult(items.All.FirstOrDefault(i => i.Id == wishlistItemId)?.GiftClaim);

        public Task<GiftClaim> CreateAsync(Guid wishlistItemId, Guid claimedByUserId, DateTime claimedAt, DateTime expiresAt, CancellationToken ct = default)
        {
            var item = items.All.Single(i => i.Id == wishlistItemId);
            if (item.GiftClaim is not null) throw new InvalidOperationException("Unique index: item already has a claim.");
            item.GiftClaim = new GiftClaim
            {
                Id = Guid.NewGuid(), WishlistItemId = wishlistItemId, ClaimedByUserId = claimedByUserId,
                Status = GiftClaimStatus.Reserved, ClaimedAt = claimedAt, ExpiresAt = expiresAt,
            };
            return Task.FromResult(item.GiftClaim);
        }

        public Task UpdateAsync(GiftClaim claim, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(Guid claimId, CancellationToken ct = default)
        {
            foreach (var item in items.All.Where(i => i.GiftClaim?.Id == claimId)) item.GiftClaim = null;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ReservationNotice>> GetReminderDueAsync(DateTime now, DateTime endsBy, CancellationToken ct = default) =>
            Notices(c => c.Status == GiftClaimStatus.Reserved && c.ReminderSentAt == null && c.ExpiresAt > now && c.ExpiresAt <= endsBy);

        public Task<IReadOnlyList<ReservationNotice>> GetExpiredAsync(DateTime now, CancellationToken ct = default) =>
            Notices(c => c.Status == GiftClaimStatus.Reserved && c.ExpiresAt <= now);

        public Task MarkReminderSentAsync(Guid claimId, DateTime sentAt, CancellationToken ct = default)
        {
            All.Single(x => x.Claim.Id == claimId).Claim.ReminderSentAt = sentAt;
            return Task.CompletedTask;
        }

        private Task<IReadOnlyList<ReservationNotice>> Notices(Func<GiftClaim, bool> match) =>
            Task.FromResult<IReadOnlyList<ReservationNotice>>(All.Where(x => match(x.Claim))
                .Select(x => new ReservationNotice(x.Claim.Id, x.Claim.ClaimedByUserId, x.Item.OwnerId, x.Item.Title, x.Claim.ExpiresAt!.Value))
                .ToList());
    }

    private sealed class SharedCircle : IGroupRepository
    {
        public Task<bool> SharesGroupAsync(Guid userId1, Guid userId2, CancellationToken ct = default) => Task.FromResult(true);

        public Task<Group?> GetByIdAsync(Guid groupId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddMemberAsync(Guid groupId, Guid userId, GroupMemberRole role, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Group> CreateAsync(string name, Guid createdByUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<GroupMember>> GetMembersAsync(Guid groupId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> GetMemberCountAsync(Guid groupId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<GroupMemberRole?> GetMemberRoleAsync(Guid groupId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Group>> GetUserGroupsAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
