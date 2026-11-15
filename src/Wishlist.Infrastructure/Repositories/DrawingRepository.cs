using Microsoft.EntityFrameworkCore;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Repositories;

public class DrawingRepository(WishlistDbContext db) : IDrawingRepository
{
    public async Task<Group> CreateAsync(string title, Guid organizerId, DateOnly endDate, string? budget, DateTime now, CancellationToken ct = default)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = title,
            CreatedByUserId = organizerId,
            CreatedAt = now,
            Kind = GroupKind.Drawing,
        };
        db.Groups.Add(group);
        db.Drawings.Add(new Drawing
        {
            GroupId = group.Id,
            OrganizerId = organizerId,
            EndDate = endDate,
            Budget = budget,
            CreatedAt = now,
        });
        await db.SaveChangesAsync(ct);
        return group;
    }

    public async Task<DrawingState?> GetAsync(Guid groupId, CancellationToken ct = default)
    {
        var drawing = await db.Drawings.AsNoTracking().FirstOrDefaultAsync(d => d.GroupId == groupId, ct);
        if (drawing is null) return null;
        var group = await db.Groups.AsNoTracking().FirstAsync(g => g.Id == groupId, ct);
        var participants = await db.DrawingParticipants.AsNoTracking()
            .Where(p => p.GroupId == groupId)
            .OrderBy(p => p.CreatedAt).ThenBy(p => p.Name)
            .ToListAsync(ct);
        var exclusions = await db.DrawingExclusions.AsNoTracking().Where(x => x.GroupId == groupId).ToListAsync(ct);
        return new DrawingState(group, drawing, participants, exclusions);
    }

    public async Task<DrawingParticipant?> GetParticipantByCodeAsync(string code, CancellationToken ct = default) =>
        await db.DrawingParticipants.AsNoTracking().FirstOrDefaultAsync(p => p.InviteCode == code, ct);

    public async Task AddParticipantsAsync(IReadOnlyCollection<DrawingParticipant> participants, CancellationToken ct = default)
    {
        db.DrawingParticipants.AddRange(participants);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateParticipantAsync(DrawingParticipant participant, CancellationToken ct = default) =>
        await db.DrawingParticipants
            .Where(p => p.Id == participant.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.UserId, participant.UserId)
                .SetProperty(p => p.Status, participant.Status)
                .SetProperty(p => p.AnsweredAt, participant.AnsweredAt), ct);

    public async Task RemoveParticipantAsync(Guid participantId, CancellationToken ct = default)
    {
        await db.DrawingExclusions
            .Where(x => x.FirstParticipantId == participantId || x.SecondParticipantId == participantId)
            .ExecuteDeleteAsync(ct);
        await db.DrawingParticipants.Where(p => p.Id == participantId).ExecuteDeleteAsync(ct);
    }

    public async Task SetExclusionsAsync(Guid groupId, IReadOnlyCollection<DrawingExclusion> exclusions, CancellationToken ct = default)
    {
        await db.DrawingExclusions.Where(x => x.GroupId == groupId).ExecuteDeleteAsync(ct);
        db.DrawingExclusions.AddRange(exclusions);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> SaveDrawAsync(Guid groupId, IReadOnlyCollection<DrawingAssignment> assignments, DateTime now, CancellationToken ct = default)
    {
        // Only the first of two simultaneous draws gets past this guard.
        var marked = await db.Drawings
            .Where(d => d.GroupId == groupId && d.DrawnAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.DrawnAt, now), ct);
        if (marked == 0) return false;

        db.DrawingAssignments.AddRange(assignments);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<DrawingAssignment?> GetAssignmentByGiverAsync(Guid giverParticipantId, CancellationToken ct = default) =>
        await db.DrawingAssignments.AsNoTracking().FirstOrDefaultAsync(a => a.GiverParticipantId == giverParticipantId, ct);

    public async Task<DrawingAssignment?> GetAssignmentByReceiverAsync(Guid receiverParticipantId, CancellationToken ct = default) =>
        await db.DrawingAssignments.AsNoTracking().FirstOrDefaultAsync(a => a.ReceiverParticipantId == receiverParticipantId, ct);

    public async Task SetCancelledAsync(Guid groupId, DateTime? cancelledAt, CancellationToken ct = default) =>
        await db.Drawings
            .Where(d => d.GroupId == groupId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.CancelledAt, cancelledAt), ct);

    public async Task<IReadOnlyList<DrawingForUser>> GetForUserAsync(Guid userId, DateTime now, CancellationToken ct = default)
    {
        var rows = await db.Drawings.AsNoTracking()
            .Join(db.Groups, d => d.GroupId, g => g.Id, (d, g) => new { Drawing = d, Group = g })
            .Select(x => new
            {
                x.Drawing,
                x.Group,
                Me = db.DrawingParticipants.FirstOrDefault(p => p.GroupId == x.Group.Id && p.UserId == userId),
            })
            .Where(x => x.Drawing.OrganizerId == userId
                || (x.Me != null && x.Me.Status != DrawingParticipantStatus.Declined))
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(now);
        return rows
            .Select(x => new DrawingForUser(x.Group, x.Drawing, x.Me, x.Drawing.OrganizerId == userId))
            .Where(x => x.IsOrganizer
                ? x.Drawing.IsVisibleToOrganizer(now)
                : x.Drawing.IsActive(today))
            .ToList();
    }

    public async Task<IReadOnlyList<DrawnRecipient>> GetMyRecipientsAsync(Guid userId, DateOnly today, CancellationToken ct = default)
    {
        var oldestActiveEnd = today.AddMonths(-Drawing.VisibleMonths);
        var rows = await db.DrawingParticipants.AsNoTracking()
            .Where(giver => giver.UserId == userId && giver.Status != DrawingParticipantStatus.Declined)
            .Join(db.Drawings.Where(d => d.CancelledAt == null && d.EndDate >= oldestActiveEnd),
                giver => giver.GroupId, d => d.GroupId, (giver, d) => giver)
            .Join(db.DrawingAssignments, giver => giver.Id, a => a.GiverParticipantId, (giver, a) => new { giver, a })
            .Join(db.DrawingParticipants, x => x.a.ReceiverParticipantId, r => r.Id, (x, receiver) => new { x.giver, receiver })
            .Join(db.Groups, x => x.giver.GroupId, g => g.Id, (x, g) => new { x.giver, x.receiver, g.Id, g.Name })
            .ToListAsync(ct);

        // A receiver who joined shows their own name.
        var joinedIds = rows.Where(r => r.receiver.Status == DrawingParticipantStatus.Joined && r.receiver.UserId != null)
            .Select(r => r.receiver.UserId!.Value).Distinct().ToList();
        var names = await db.Users.Where(u => joinedIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);

        return rows.Select(r =>
        {
            var joined = r.receiver.Status == DrawingParticipantStatus.Joined;
            var name = joined && r.receiver.UserId is { } id && names.TryGetValue(id, out var n) ? n : r.receiver.Name;
            return new DrawnRecipient(r.Id, r.Name, r.receiver.Id, name, r.receiver.UserId, joined,
                r.receiver.IsInvitedByEmail, r.giver.Status == DrawingParticipantStatus.Joined);
        }).ToList();
    }

    public async Task<int> DeleteDueAsync(DateTime now, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(now);
        var oldestActiveEnd = today.AddMonths(-Drawing.VisibleMonths);
        var cancelledBefore = now.AddMonths(-Drawing.VisibleMonths);
        var due = db.Drawings
            .Where(d => d.EndDate < oldestActiveEnd || (d.CancelledAt != null && d.CancelledAt <= cancelledBefore))
            .Select(d => d.GroupId);
        // The drawing, its participants, exclusions, assignments and memberships go with the circle.
        return await db.Groups.Where(g => g.Kind == GroupKind.Drawing && due.Contains(g.Id)).ExecuteDeleteAsync(ct);
    }
}
