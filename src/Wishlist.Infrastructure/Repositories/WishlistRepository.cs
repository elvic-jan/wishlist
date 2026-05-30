using Microsoft.EntityFrameworkCore;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Repositories;

public class WishlistRepository(WishlistDbContext db) : IWishlistRepository
{
    public async Task<IReadOnlyList<WishlistItem>> GetByOwnerAsync(Guid ownerId, bool includeClaims, CancellationToken ct = default)
    {
        var query = db.WishlistItems.Where(w => w.OwnerId == ownerId);
        if (includeClaims)
            query = query.Include(w => w.GiftClaim);
        return await query.OrderBy(w => w.CreatedAt).ToListAsync(ct);
    }

    public async Task<WishlistItem?> GetByIdAsync(Guid itemId, bool includeClaim, CancellationToken ct = default)
    {
        var query = db.WishlistItems.Where(w => w.Id == itemId);
        if (includeClaim)
            query = query.Include(w => w.GiftClaim);
        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task<WishlistItem> CreateAsync(Guid ownerId, string title, string? description, string? url, decimal? price, CancellationToken ct = default)
    {
        var item = new WishlistItem
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = title,
            Description = description,
            Url = url,
            Price = price,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.WishlistItems.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task UpdateAsync(WishlistItem item, CancellationToken ct = default)
    {
        item.UpdatedAt = DateTime.UtcNow;
        db.WishlistItems.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid itemId, CancellationToken ct = default)
    {
        var item = await db.WishlistItems.FindAsync([itemId], ct);
        if (item is not null)
        {
            db.WishlistItems.Remove(item);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetGroupMemberSummariesAsync(Guid groupId, Guid excludeUserId, CancellationToken ct = default)
    {
        var results = await db.GroupMembers
            .Where(m => m.GroupId == groupId && m.UserId != excludeUserId)
            .Join(db.Users, m => m.UserId, u => u.Id, (m, u) => new { u.Id, u.DisplayName })
            .ToListAsync(ct);

        return results.Select(r => (r.Id, r.DisplayName)).ToList();
    }
}
