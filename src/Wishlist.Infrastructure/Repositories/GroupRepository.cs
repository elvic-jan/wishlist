using Microsoft.EntityFrameworkCore;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Repositories;

public class GroupRepository(WishlistDbContext db) : IGroupRepository
{
    public async Task<Group?> GetByIdAsync(Guid groupId, CancellationToken ct = default)
        => await db.Groups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == groupId, ct);

    public async Task<Group> CreateAsync(string name, Guid createdByUserId, CancellationToken ct = default)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
        db.Groups.Add(group);
        await db.SaveChangesAsync(ct);
        return group;
    }

    public async Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default)
        => await db.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId, ct);

    public async Task<IReadOnlyList<GroupMember>> GetMembersAsync(Guid groupId, CancellationToken ct = default)
    {
        var rows = await db.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Join(db.Users, m => m.UserId, u => u.Id, (m, u) => new { m, u.DisplayName })
            .ToListAsync(ct);

        return rows.Select(r =>
        {
            r.m.DisplayName = r.DisplayName;
            return r.m;
        }).ToList();
    }

    public async Task AddMemberAsync(Guid groupId, Guid userId, GroupMemberRole role, CancellationToken ct = default)
    {
        var member = new GroupMember
        {
            GroupId = groupId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
        db.GroupMembers.Add(member);
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default)
    {
        var member = await db.GroupMembers.FindAsync([groupId, userId], ct);
        if (member is not null)
        {
            db.GroupMembers.Remove(member);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> GetMemberCountAsync(Guid groupId, CancellationToken ct = default)
        => await db.GroupMembers.CountAsync(m => m.GroupId == groupId, ct);

    public async Task<GroupMemberRole?> GetMemberRoleAsync(Guid groupId, Guid userId, CancellationToken ct = default)
    {
        var member = await db.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, ct);
        return member?.Role;
    }

    public async Task<bool> SharesGroupAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
        => await db.GroupMembers
            .Where(m => m.UserId == userId1)
            .Join(db.GroupMembers.Where(m => m.UserId == userId2),
                m1 => m1.GroupId, m2 => m2.GroupId, (m1, m2) => m1.GroupId)
            .AnyAsync(ct);

    public async Task<IReadOnlyList<Group>> GetUserGroupsAsync(Guid userId, CancellationToken ct = default)
        => await db.GroupMembers
            .Where(m => m.UserId == userId)
            .Join(db.Groups, m => m.GroupId, g => g.Id, (m, g) => g)
            .ToListAsync(ct);
}
