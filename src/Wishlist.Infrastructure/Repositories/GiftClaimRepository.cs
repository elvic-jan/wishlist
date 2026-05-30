using Microsoft.EntityFrameworkCore;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Repositories;

public class GiftClaimRepository(WishlistDbContext db) : IGiftClaimRepository
{
    public async Task<GiftClaim?> GetByItemIdAsync(Guid wishlistItemId, CancellationToken ct = default)
        => await db.GiftClaims.FirstOrDefaultAsync(c => c.WishlistItemId == wishlistItemId, ct);

    public async Task<GiftClaim> CreateAsync(Guid wishlistItemId, Guid claimedByUserId, CancellationToken ct = default)
    {
        var claim = new GiftClaim
        {
            Id = Guid.NewGuid(),
            WishlistItemId = wishlistItemId,
            ClaimedByUserId = claimedByUserId,
            Status = GiftClaimStatus.Reserved,
            ClaimedAt = DateTime.UtcNow
        };
        db.GiftClaims.Add(claim);
        await db.SaveChangesAsync(ct);
        return claim;
    }

    public async Task UpdateStatusAsync(Guid claimId, GiftClaimStatus status, CancellationToken ct = default)
    {
        var claim = await db.GiftClaims.FindAsync([claimId], ct);
        if (claim is not null)
        {
            claim.Status = status;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task DeleteAsync(Guid claimId, CancellationToken ct = default)
    {
        var claim = await db.GiftClaims.FindAsync([claimId], ct);
        if (claim is not null)
        {
            db.GiftClaims.Remove(claim);
            await db.SaveChangesAsync(ct);
        }
    }
}
