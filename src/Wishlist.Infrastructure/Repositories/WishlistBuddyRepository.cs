using Microsoft.EntityFrameworkCore;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Repositories;

public class WishlistBuddyRepository(WishlistDbContext db, TimeProvider time) : IWishlistBuddyRepository
{
    public async Task<bool> AddAsync(Guid ownerId, Guid buddyId, DateTime now, CancellationToken ct = default)
    {
        if (await db.WishlistBuddies.AnyAsync(b => b.OwnerId == ownerId && b.BuddyId == buddyId, ct))
            return false;

        db.WishlistBuddies.Add(new WishlistBuddy { OwnerId = ownerId, BuddyId = buddyId, CreatedAt = now });
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            // Added at the same moment by another request.
            db.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task RemoveAsync(Guid ownerId, Guid buddyId, CancellationToken ct = default) =>
        await db.WishlistBuddies
            .Where(b => b.OwnerId == ownerId && b.BuddyId == buddyId)
            .ExecuteDeleteAsync(ct);

    public async Task<bool> IsActiveBuddyAsync(Guid ownerId, Guid buddyId, CancellationToken ct = default) =>
        await ActiveBuddies().AnyAsync(b => b.OwnerId == ownerId && b.BuddyId == buddyId, ct);

    public async Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetBuddiesAsync(Guid ownerId, CancellationToken ct = default) =>
        await ToSummaries(ActiveBuddies().Where(b => b.OwnerId == ownerId).Select(b => b.BuddyId), ct);

    public async Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetBuddyOfAsync(Guid buddyId, CancellationToken ct = default) =>
        await ToSummaries(ActiveBuddies().Where(b => b.BuddyId == buddyId).Select(b => b.OwnerId), ct);

    public async Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetCandidatesAsync(Guid ownerId, CancellationToken ct = default)
    {
        var memberships = Connecting();
        var myGroups = memberships.Where(m => m.UserId == ownerId).Select(m => m.GroupId);
        var buddies = db.WishlistBuddies.Where(b => b.OwnerId == ownerId).Select(b => b.BuddyId);
        var ids = memberships
            .Where(m => myGroups.Contains(m.GroupId) && m.UserId != ownerId && !buddies.Contains(m.UserId))
            .Select(m => m.UserId)
            .Distinct();
        return await ToSummaries(ids, ct);
    }

    /// <summary>Buddy rows where owner and buddy share at least one circle.</summary>
    private IQueryable<WishlistBuddy> ActiveBuddies()
    {
        var memberships = Connecting();
        return db.WishlistBuddies.Where(b => memberships.Any(m1 =>
            m1.UserId == b.OwnerId &&
            memberships.Any(m2 => m2.GroupId == m1.GroupId && m2.UserId == b.BuddyId)));
    }

    private IQueryable<GroupMember> Connecting() =>
        db.ConnectingMemberships(DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime));

    private async Task<IReadOnlyList<(Guid UserId, string DisplayName)>> ToSummaries(IQueryable<Guid> userIds, CancellationToken ct)
    {
        var rows = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .OrderBy(u => u.DisplayName)
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync(ct);
        return rows.Select(r => (r.Id, r.DisplayName)).ToList();
    }
}
