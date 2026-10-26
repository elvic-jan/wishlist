using Microsoft.EntityFrameworkCore;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Repositories;

public class InvitationRepository(WishlistDbContext db) : IInvitationRepository
{
    public async Task<GroupInvitation> CreateAsync(Guid groupId, Guid createdByUserId, CancellationToken ct = default)
    {
        var invitation = new GroupInvitation
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            InviteCode = Guid.NewGuid().ToString("N"),
            CreatedByUserId = createdByUserId,
            ExpiresAt = DateTime.UtcNow.Add(GroupInvitation.Lifetime),
            CreatedAt = DateTime.UtcNow
        };
        db.GroupInvitations.Add(invitation);
        await db.SaveChangesAsync(ct);
        return invitation;
    }

    public async Task<GroupInvitation?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await db.GroupInvitations.FirstOrDefaultAsync(i => i.InviteCode == code, ct);

    public async Task MarkUsedAsync(Guid invitationId, CancellationToken ct = default)
    {
        var invitation = await db.GroupInvitations.FindAsync([invitationId], ct);
        if (invitation is not null)
        {
            invitation.UsedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
