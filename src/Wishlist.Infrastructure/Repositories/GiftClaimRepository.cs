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

    public async Task<GiftClaim> CreateAsync(Guid wishlistItemId, Guid claimedByUserId, DateTime claimedAt, DateTime expiresAt, CancellationToken ct = default)
    {
        var claim = new GiftClaim
        {
            Id = Guid.NewGuid(),
            WishlistItemId = wishlistItemId,
            ClaimedByUserId = claimedByUserId,
            Status = GiftClaimStatus.Reserved,
            ClaimedAt = claimedAt,
            ExpiresAt = expiresAt,
        };
        db.GiftClaims.Add(claim);
        await db.SaveChangesAsync(ct);
        return claim;
    }

    public async Task UpdateAsync(GiftClaim claim, CancellationToken ct = default)
    {
        var stored = await db.GiftClaims.FindAsync([claim.Id], ct);
        if (stored is null) return;

        stored.Status = claim.Status;
        stored.ExpiresAt = claim.ExpiresAt;
        stored.ReminderSentAt = claim.ReminderSentAt;
        await db.SaveChangesAsync(ct);
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

    public async Task<IReadOnlyList<ReservationNotice>> GetReminderDueAsync(DateTime now, DateTime endsBy, CancellationToken ct = default) =>
        await Notices(db.GiftClaims.Where(c =>
            c.Status == GiftClaimStatus.Reserved &&
            c.ReminderSentAt == null &&
            c.ExpiresAt > now && c.ExpiresAt <= endsBy), ct);

    public async Task<IReadOnlyList<ReservationNotice>> GetExpiredAsync(DateTime now, CancellationToken ct = default) =>
        await Notices(db.GiftClaims.Where(c => c.Status == GiftClaimStatus.Reserved && c.ExpiresAt <= now), ct);

    public async Task MarkReminderSentAsync(Guid claimId, DateTime sentAt, CancellationToken ct = default) =>
        await db.GiftClaims.Where(c => c.Id == claimId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.ReminderSentAt, sentAt), ct);

    private async Task<List<ReservationNotice>> Notices(IQueryable<GiftClaim> claims, CancellationToken ct) =>
        await claims
            .Join(db.WishlistItems, c => c.WishlistItemId, i => i.Id,
                (c, i) => new ReservationNotice(c.Id, c.ClaimedByUserId, i.OwnerId, i.Title, c.ExpiresAt!.Value))
            .AsNoTracking()
            .ToListAsync(ct);
}
