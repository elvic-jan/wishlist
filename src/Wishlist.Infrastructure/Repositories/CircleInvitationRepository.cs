using Microsoft.EntityFrameworkCore;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Repositories;

public class CircleInvitationRepository(WishlistDbContext db) : ICircleInvitationRepository
{
    public async Task<IReadOnlyList<InviteCandidateCircle>> GetCandidatesAsync(Guid groupId, Guid callerId, CancellationToken ct = default)
    {
        var targetMembers = db.GroupMembers.Where(m => m.GroupId == groupId).Select(m => m.UserId);
        var invited = db.CircleInvitations.Where(i => i.GroupId == groupId).Select(i => i.InviteeId);

        var rows = await db.GroupMembers
            .Where(mine => mine.UserId == callerId && mine.GroupId != groupId)
            .Join(db.Groups, mine => mine.GroupId, g => g.Id, (mine, g) => g)
            .Join(db.GroupMembers, g => g.Id, m => m.GroupId, (g, m) => new { Group = g, m.UserId })
            .Where(x => x.UserId != callerId && !targetMembers.Contains(x.UserId))
            .Join(db.Users, x => x.UserId, u => u.Id, (x, u) => new
            {
                GroupId = x.Group.Id,
                GroupName = x.Group.Name,
                u.Id,
                u.DisplayName,
                Invited = invited.Contains(u.Id),
            })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => (r.GroupId, r.GroupName))
            .OrderBy(g => g.Key.GroupName, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => new InviteCandidateCircle(
                g.Key.GroupId,
                g.Key.GroupName,
                g.OrderBy(r => r.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .Select(r => new InviteCandidate(r.Id, r.DisplayName, r.Invited))
                    .ToList()))
            .ToList();
    }

    public async Task<IReadOnlySet<Guid>> GetInvitedAsync(Guid groupId, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default) =>
        (await db.CircleInvitations
            .Where(i => i.GroupId == groupId && userIds.Contains(i.InviteeId))
            .Select(i => i.InviteeId)
            .ToListAsync(ct)).ToHashSet();

    public async Task<IReadOnlySet<Guid>> GetCircleMatesAsync(Guid userId, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        var myGroups = db.GroupMembers.Where(m => m.UserId == userId).Select(m => m.GroupId);
        return (await db.GroupMembers
            .Where(m => myGroups.Contains(m.GroupId) && userIds.Contains(m.UserId))
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync(ct)).ToHashSet();
    }

    public async Task<IReadOnlySet<Guid>> GetMembersAsync(Guid groupId, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default) =>
        (await db.GroupMembers
            .Where(m => m.GroupId == groupId && userIds.Contains(m.UserId))
            .Select(m => m.UserId)
            .ToListAsync(ct)).ToHashSet();

    public async Task<IReadOnlyList<Guid>> CreateAsync(Guid groupId, Guid invitedById, IReadOnlyCollection<Guid> inviteeIds, DateTime now, CancellationToken ct = default)
    {
        var added = new List<Guid>();
        foreach (var inviteeId in inviteeIds)
        {
            db.CircleInvitations.Add(new CircleInvitation
            {
                Id = Guid.NewGuid(), GroupId = groupId, InviteeId = inviteeId, InvitedById = invitedById, CreatedAt = now,
            });
            try
            {
                await db.SaveChangesAsync(ct);
                added.Add(inviteeId);
            }
            catch (DbUpdateException)
            {
                // Invited at the same moment by someone else (unique index).
                db.ChangeTracker.Clear();
            }
        }
        return added;
    }

    public async Task<IReadOnlyList<MyCircleInvitation>> GetForInviteeAsync(Guid inviteeId, CancellationToken ct = default)
    {
        var rows = await db.CircleInvitations
            .Where(i => i.InviteeId == inviteeId)
            .Join(db.Groups, i => i.GroupId, g => g.Id, (i, g) => new { i, g.Name })
            .GroupJoin(db.Users, x => x.i.InvitedById, u => u.Id, (x, us) => new { x.i, x.Name, us })
            .SelectMany(x => x.us.DefaultIfEmpty(), (x, u) => new
            {
                x.i.Id, x.i.GroupId, x.Name, InvitedByName = u == null ? null : u.DisplayName, x.i.CreatedAt,
            })
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(r => new MyCircleInvitation(r.Id, r.GroupId, r.Name, r.InvitedByName, r.CreatedAt)).ToList();
    }

    public async Task<CircleInvitation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.CircleInvitations.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        await db.CircleInvitations.Where(i => i.Id == id).ExecuteDeleteAsync(ct);
}
