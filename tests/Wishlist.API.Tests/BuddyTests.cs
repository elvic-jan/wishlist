using Microsoft.Extensions.Logging.Abstractions;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Buddies;
using Wishlist.Application.UseCases.Wishlists;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.API.Tests;

/// <summary>Wishlist buddies: picking them, and their full control over the owner's list.</summary>
public class BuddyTests
{
    [Fact]
    public async Task Adding_a_buddy_sends_one_email_and_lists_them()
    {
        var w = new World();

        var dto = await w.Manage.AddAsync(w.Owner.Id, w.Buddy.Id);
        await w.Manage.AddAsync(w.Owner.Id, w.Buddy.Id); // again: no duplicate, no second email

        Assert.Equal(w.Buddy.Id, dto.UserId);
        Assert.Equal([w.Buddy.Id], (await w.Manage.GetBuddiesAsync(w.Owner.Id)).Select(b => b.UserId));
        Assert.Equal([w.Owner.Id], (await w.Manage.GetBuddyOfAsync(w.Buddy.Id)).Select(b => b.UserId));
        var email = Assert.Single(w.Store.EmailsTo(w.Buddy.Email));
        Assert.Contains("made you a buddy", email.Subject);
        Assert.Contains($"https://giftcircles.test/wishlist/{w.Owner.Id}", email.PlainTextBody);
    }

    [Fact]
    public async Task Candidates_are_circle_members_minus_self_and_existing_buddies()
    {
        var w = new World();
        await w.Manage.AddAsync(w.Owner.Id, w.Buddy.Id);

        var candidates = (await w.Manage.GetCandidatesAsync(w.Owner.Id)).Select(c => c.UserId).ToList();

        Assert.Equal([w.Giver.Id], candidates);
    }

    [Fact]
    public async Task Cannot_pick_yourself_or_someone_outside_your_circles()
    {
        var w = new World();

        await Assert.ThrowsAsync<DomainException>(() => w.Manage.AddAsync(w.Owner.Id, w.Owner.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => w.Manage.AddAsync(w.Owner.Id, w.Stranger.Id));
        Assert.Empty(await w.Manage.GetBuddiesAsync(w.Owner.Id));
        Assert.Empty(w.Store.SentEmails);
    }

    [Fact]
    public async Task Email_failure_still_adds_the_buddy()
    {
        var w = new World(failEmails: true);

        await w.Manage.AddAsync(w.Owner.Id, w.Buddy.Id);

        Assert.Single(await w.Manage.GetBuddiesAsync(w.Owner.Id));
    }

    [Fact]
    public async Task Buddy_adds_a_wish_that_belongs_to_the_owner_and_says_who_added_it()
    {
        var w = await World.WithBuddyAsync();

        var dto = await w.Add.ExecuteAsBuddyAsync(w.Buddy.Id, w.Owner.Id, new AddWishlistItemRequest("Lego", null, ["https://shop.test/lego"]));

        Assert.Equal(w.Owner.Id, dto.OwnerId);
        Assert.Equal(new AddedByDto(w.Buddy.Id, w.Buddy.DisplayName), dto.AddedBy);
        var own = Assert.Single(await new GetOwnWishlistUseCase(w.Items).ExecuteAsync(w.Owner.Id));
        Assert.Equal(w.Buddy.Id, own.AddedBy?.UserId);
    }

    [Fact]
    public async Task Owner_never_sees_the_reservation_on_a_buddy_added_wish()
    {
        var w = await World.WithBuddyAsync();
        var dto = await w.Add.ExecuteAsBuddyAsync(w.Buddy.Id, w.Owner.Id, new AddWishlistItemRequest("Lego", null, null));
        w.Items.All.Single(i => i.Id == dto.Id).GiftClaim = new GiftClaim
        {
            Id = Guid.NewGuid(), WishlistItemId = dto.Id, ClaimedByUserId = w.Giver.Id,
            Status = GiftClaimStatus.Bought, ClaimedAt = DateTime.UtcNow,
        };

        var own = Assert.Single(await new GetOwnWishlistUseCase(w.Items).ExecuteAsync(w.Owner.Id));
        var asGiver = Assert.Single(await new GetMemberWishlistUseCase(w.Items, w.Groups, TimeProvider.System).ExecuteAsync(w.Giver.Id, w.Owner.Id));

        Assert.Null(own.Claim);
        Assert.NotNull(asGiver.Claim);
        Assert.Equal(w.Buddy.Id, asGiver.AddedBy?.UserId);
    }

    [Fact]
    public async Task Buddy_edits_and_deletes_the_owners_own_wishes()
    {
        var w = await World.WithBuddyAsync();
        var mine = await w.Add.ExecuteAsync(w.Owner.Id, new AddWishlistItemRequest("Book", null, null));
        var other = await w.Add.ExecuteAsync(w.Owner.Id, new AddWishlistItemRequest("Scarf", null, null));

        var edited = await w.Edit.ExecuteAsBuddyAsync(w.Buddy.Id, w.Owner.Id, mine.Id, new EditWishlistItemRequest("Book, 2nd edition", null, ["https://shop.test/book"]));
        await w.Delete.ExecuteAsBuddyAsync(w.Buddy.Id, w.Owner.Id, other.Id);

        Assert.Equal("Book, 2nd edition", edited.Title);
        Assert.Equal(["https://shop.test/book"], edited.Urls);
        Assert.Null(edited.AddedBy); // the owner added it; an edit doesn't change that
        Assert.Equal([mine.Id], w.Items.All.Select(i => i.Id));
    }

    [Fact]
    public async Task Owner_edits_and_deletes_a_buddy_added_wish()
    {
        var w = await World.WithBuddyAsync();
        var added = await w.Add.ExecuteAsBuddyAsync(w.Buddy.Id, w.Owner.Id, new AddWishlistItemRequest("Lego", null, null));

        var edited = await w.Edit.ExecuteAsync(w.Owner.Id, added.Id, new EditWishlistItemRequest("Lego Technic", null, null));
        Assert.Equal(w.Buddy.Id, edited.AddedBy?.UserId);

        await w.Delete.ExecuteAsync(w.Owner.Id, added.Id);
        Assert.Empty(w.Items.All);
    }

    [Fact]
    public async Task Non_buddy_cannot_change_the_list()
    {
        var w = await World.WithBuddyAsync();
        var item = await w.Add.ExecuteAsync(w.Owner.Id, new AddWishlistItemRequest("Book", null, null));

        await Assert.ThrowsAsync<ForbiddenException>(() => w.Add.ExecuteAsBuddyAsync(w.Giver.Id, w.Owner.Id, new AddWishlistItemRequest("X", null, null)));
        await Assert.ThrowsAsync<ForbiddenException>(() => w.Edit.ExecuteAsBuddyAsync(w.Giver.Id, w.Owner.Id, item.Id, new EditWishlistItemRequest("X", null, null)));
        await Assert.ThrowsAsync<ForbiddenException>(() => w.Delete.ExecuteAsBuddyAsync(w.Giver.Id, w.Owner.Id, item.Id));
        // The owner's own endpoints still reject other people's items.
        await Assert.ThrowsAsync<ForbiddenException>(() => w.Edit.ExecuteAsync(w.Buddy.Id, item.Id, new EditWishlistItemRequest("X", null, null)));
        Assert.Equal("Book", Assert.Single(w.Items.All).Title);
    }

    [Fact]
    public async Task Buddy_loses_rights_after_leaving_the_shared_circles_and_regains_them_on_return()
    {
        var w = await World.WithBuddyAsync();
        w.Store.Members.TryRemove((w.Circle.Id, w.Buddy.Id), out _);

        await Assert.ThrowsAsync<ForbiddenException>(() => w.Add.ExecuteAsBuddyAsync(w.Buddy.Id, w.Owner.Id, new AddWishlistItemRequest("X", null, null)));
        Assert.Empty(await w.Manage.GetBuddiesAsync(w.Owner.Id));
        Assert.Empty(await w.Manage.GetBuddyOfAsync(w.Buddy.Id));

        w.Join(w.Buddy);
        await w.Add.ExecuteAsBuddyAsync(w.Buddy.Id, w.Owner.Id, new AddWishlistItemRequest("X", null, null));
    }

    [Fact]
    public async Task Removing_a_buddy_keeps_their_wishes()
    {
        var w = await World.WithBuddyAsync();
        await w.Add.ExecuteAsBuddyAsync(w.Buddy.Id, w.Owner.Id, new AddWishlistItemRequest("Lego", null, null));

        await w.Manage.RemoveAsync(w.Owner.Id, w.Buddy.Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => w.Add.ExecuteAsBuddyAsync(w.Buddy.Id, w.Owner.Id, new AddWishlistItemRequest("X", null, null)));
        var kept = Assert.Single(await new GetOwnWishlistUseCase(w.Items).ExecuteAsync(w.Owner.Id));
        Assert.Equal(w.Buddy.Id, kept.AddedBy?.UserId);
    }

    [Fact]
    public async Task Buddy_routes_reject_yourself_wrong_owner_and_invalid_wishes()
    {
        var w = await World.WithBuddyAsync();
        var giversItem = await w.Add.ExecuteAsync(w.Giver.Id, new AddWishlistItemRequest("Not the owner's", null, null));

        await Assert.ThrowsAsync<DomainException>(() => w.Add.ExecuteAsBuddyAsync(w.Owner.Id, w.Owner.Id, new AddWishlistItemRequest("X", null, null)));
        await Assert.ThrowsAsync<NotFoundException>(() => w.Delete.ExecuteAsBuddyAsync(w.Buddy.Id, w.Owner.Id, giversItem.Id));
        await Assert.ThrowsAsync<DomainException>(() => w.Add.ExecuteAsBuddyAsync(w.Buddy.Id, w.Owner.Id, new AddWishlistItemRequest(" ", null, null)));
        await Assert.ThrowsAsync<DomainException>(() => w.Add.ExecuteAsBuddyAsync(w.Buddy.Id, w.Owner.Id,
            new AddWishlistItemRequest("X", null, Enumerable.Range(0, 11).Select(i => $"https://shop.test/{i}").ToList())));
        Assert.Single(w.Items.All);
    }

    private sealed class World
    {
        public InMemoryStore Store { get; } = new();
        public Items Items { get; }
        public Groups Groups { get; }
        public Group Circle { get; }
        public User Owner { get; }
        public User Buddy { get; }
        public User Giver { get; }
        public User Stranger { get; }
        public ManageBuddiesUseCase Manage { get; }
        public AddWishlistItemUseCase Add { get; }
        public EditWishlistItemUseCase Edit { get; }
        public DeleteWishlistItemUseCase Delete { get; }

        public World(bool failEmails = false)
        {
            Owner = Named("owner@example.test", "Owner");
            Buddy = Named("buddy@example.test", "Buddy");
            Giver = Named("giver@example.test", "Giver");
            Stranger = Named("stranger@example.test", "Stranger");
            Circle = Store.AddGroup("Family");
            Join(Owner);
            Join(Buddy);
            Join(Giver);

            Items = new Items(Store);
            Groups = new Groups(Store);
            var buddies = new Buddies(Store, Groups);
            IEmailSender sender = failEmails ? new FailingSender() : new CapturingEmailSender(Store);
            var emails = new BuddyEmails(sender, new Urls(), NullLogger<BuddyEmails>.Instance);
            Manage = new ManageBuddiesUseCase(buddies, Groups, new InMemoryUsers(Store), emails, TimeProvider.System);
            var access = new WishlistAccess(buddies);
            Add = new AddWishlistItemUseCase(Items, access);
            Edit = new EditWishlistItemUseCase(Items, access);
            Delete = new DeleteWishlistItemUseCase(Items, access);
        }

        public static async Task<World> WithBuddyAsync()
        {
            var w = new World();
            await w.Manage.AddAsync(w.Owner.Id, w.Buddy.Id);
            return w;
        }

        public void Join(User user) =>
            Store.Members[(Circle.Id, user.Id)] = new GroupMember { GroupId = Circle.Id, UserId = user.Id, Role = GroupMemberRole.Member };

        private User Named(string email, string name)
        {
            var user = Store.AddUser(email);
            user.DisplayName = name;
            return user;
        }
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

    private sealed class Groups(InMemoryStore store) : IGroupRepository
    {
        public Task<bool> SharesGroupAsync(Guid userId1, Guid userId2, CancellationToken ct = default) =>
            Task.FromResult(Shares(userId1, userId2));

        public bool Shares(Guid a, Guid b) =>
            store.Members.Keys.Where(k => k.UserId == a).Select(k => k.GroupId)
                .Intersect(store.Members.Keys.Where(k => k.UserId == b).Select(k => k.GroupId)).Any();

        public IEnumerable<Guid> CircleMatesOf(Guid userId)
        {
            var mine = store.Members.Keys.Where(k => k.UserId == userId).Select(k => k.GroupId).ToHashSet();
            return store.Members.Keys.Where(k => mine.Contains(k.GroupId) && k.UserId != userId).Select(k => k.UserId).Distinct();
        }

        public Task<Group?> GetByIdAsync(Guid groupId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Group> CreateAsync(string name, Guid createdByUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<GroupMember>> GetMembersAsync(Guid groupId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddMemberAsync(Guid groupId, Guid userId, GroupMemberRole role, CancellationToken ct = default) => throw new NotSupportedException();
        public Task RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> GetMemberCountAsync(Guid groupId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<GroupMemberRole?> GetMemberRoleAsync(Guid groupId, Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Group>> GetUserGroupsAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<(Guid UserId, string DisplayName, Guid GroupId, string GroupName)>> GetCircleMatesAsync(Guid userId, CancellationToken ct = default) => throw new NotSupportedException();
    }

    /// <summary>Same rules as the EF repository: a buddy only counts while they share a circle with the owner.</summary>
    private sealed class Buddies(InMemoryStore store, Groups groups) : IWishlistBuddyRepository
    {
        private readonly HashSet<(Guid Owner, Guid Buddy)> _rows = [];

        public Task<bool> AddAsync(Guid ownerId, Guid buddyId, DateTime now, CancellationToken ct = default) =>
            Task.FromResult(_rows.Add((ownerId, buddyId)));

        public Task RemoveAsync(Guid ownerId, Guid buddyId, CancellationToken ct = default)
        {
            _rows.Remove((ownerId, buddyId));
            return Task.CompletedTask;
        }

        public Task<bool> IsActiveBuddyAsync(Guid ownerId, Guid buddyId, CancellationToken ct = default) =>
            Task.FromResult(Active().Contains((ownerId, buddyId)));

        public Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetBuddiesAsync(Guid ownerId, CancellationToken ct = default) =>
            Summaries(Active().Where(r => r.Owner == ownerId).Select(r => r.Buddy));

        public Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetBuddyOfAsync(Guid buddyId, CancellationToken ct = default) =>
            Summaries(Active().Where(r => r.Buddy == buddyId).Select(r => r.Owner));

        public Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetCandidatesAsync(Guid ownerId, CancellationToken ct = default) =>
            Summaries(groups.CircleMatesOf(ownerId).Where(id => !_rows.Contains((ownerId, id))));

        private List<(Guid Owner, Guid Buddy)> Active() => _rows.Where(r => groups.Shares(r.Owner, r.Buddy)).ToList();

        private Task<IReadOnlyList<(Guid UserId, string DisplayName)>> Summaries(IEnumerable<Guid> ids) =>
            Task.FromResult<IReadOnlyList<(Guid, string)>>(ids.Select(id => (id, store.Users[id].DisplayName)).ToList());
    }

    /// <summary>Wishes in memory; fills the adder's name like the EF repository.</summary>
    private sealed class Items(InMemoryStore store) : IWishlistRepository
    {
        public List<WishlistItem> All { get; } = [];

        public Task<IReadOnlyList<WishlistItem>> GetByOwnerAsync(Guid ownerId, bool includeClaims, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WishlistItem>>(All.Where(i => i.OwnerId == ownerId).Select(Named).ToList());

        public Task<WishlistItem?> GetByIdAsync(Guid itemId, bool includeClaim, CancellationToken ct = default) =>
            Task.FromResult(All.Where(i => i.Id == itemId).Select(Named).FirstOrDefault());

        public Task<WishlistItem> CreateAsync(Guid ownerId, string title, string? description, IReadOnlyList<string> urls, CancellationToken ct = default, Guid? addedById = null)
        {
            var item = new WishlistItem
            {
                Id = Guid.NewGuid(), OwnerId = ownerId, AddedById = addedById, Title = title, Description = description,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                Urls = urls.Select((u, n) => new WishlistItemUrl { Id = Guid.NewGuid(), Url = u, DisplayOrder = n }).ToList(),
            };
            All.Add(item);
            return Task.FromResult(Named(item));
        }

        public Task UpdateAsync(WishlistItem item, CancellationToken ct = default) => Task.CompletedTask;

        public Task ReplaceUrlsAsync(WishlistItem item, IReadOnlyList<string> urls, CancellationToken ct = default)
        {
            item.Urls = urls.Select((u, n) => new WishlistItemUrl { Id = Guid.NewGuid(), WishlistItemId = item.Id, Url = u, DisplayOrder = n }).ToList();
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid itemId, CancellationToken ct = default)
        {
            All.RemoveAll(i => i.Id == itemId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetGroupMemberSummariesAsync(Guid groupId, Guid excludeUserId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        private WishlistItem Named(WishlistItem item)
        {
            item.AddedByName = item.AddedById is { } id ? store.Users[id].DisplayName : null;
            return item;
        }
    }
}
