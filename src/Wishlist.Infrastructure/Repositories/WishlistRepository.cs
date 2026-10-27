using Microsoft.EntityFrameworkCore;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Repositories;

public class WishlistRepository(WishlistDbContext db) : IWishlistRepository
{
    public async Task<IReadOnlyList<WishlistItem>> GetByOwnerAsync(Guid ownerId, bool includeClaims, CancellationToken ct = default)
    {
        IQueryable<WishlistItem> query = db.WishlistItems.Where(w => w.OwnerId == ownerId);
        query = query.Include(w => w.Urls.OrderBy(u => u.DisplayOrder));
        if (includeClaims)
            query = query.Include(w => w.GiftClaim);
        var items = await query.OrderBy(w => w.CreatedAt).ToListAsync(ct);
        await FillAddedByNamesAsync(items, ct);
        return items;
    }

    public async Task<WishlistItem?> GetByIdAsync(Guid itemId, bool includeClaim, CancellationToken ct = default)
    {
        IQueryable<WishlistItem> query = db.WishlistItems.Where(w => w.Id == itemId);
        query = query.Include(w => w.Urls.OrderBy(u => u.DisplayOrder));
        if (includeClaim)
            query = query.Include(w => w.GiftClaim);
        var item = await query.FirstOrDefaultAsync(ct);
        if (item is not null)
            await FillAddedByNamesAsync([item], ct);
        return item;
    }

    public async Task<WishlistItem> CreateAsync(Guid ownerId, string title, string? description, IReadOnlyList<string> urls, CancellationToken ct = default, Guid? addedById = null)
    {
        var item = new WishlistItem
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            AddedById = addedById,
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        for (var i = 0; i < urls.Count; i++)
        {
            item.Urls.Add(new WishlistItemUrl
            {
                Id = Guid.NewGuid(),
                WishlistItemId = item.Id,
                Url = urls[i],
                DisplayOrder = i,
            });
        }

        db.WishlistItems.Add(item);
        await db.SaveChangesAsync(ct);
        await FillAddedByNamesAsync([item], ct);
        return item;
    }

    public async Task UpdateAsync(WishlistItem item, CancellationToken ct = default)
    {
        item.UpdatedAt = DateTime.UtcNow;
        db.WishlistItems.Update(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task ReplaceUrlsAsync(WishlistItem item, IReadOnlyList<string> urls, CancellationToken ct = default)
    {
        // Wipe existing URL rows directly in SQL so the change tracker
        // doesn't try to delete entities that the navigation collection
        // already references.
        await db.WishlistItemUrls
            .Where(u => u.WishlistItemId == item.Id)
            .ExecuteDeleteAsync(ct);

        // Detach the stale tracked copies and clear the in-memory collection
        // so EF doesn't issue spurious DELETE/UPDATE for already-gone rows.
        foreach (var oldUrl in item.Urls.ToList())
        {
            db.Entry(oldUrl).State = EntityState.Detached;
        }
        item.Urls.Clear();

        // Add the new rows via the DbSet so they are tracked as Added.
        // EF's relationship fixup will also populate the navigation
        // collection — appending here AND to item.Urls would duplicate
        // the navigation in-memory.
        for (var i = 0; i < urls.Count; i++)
        {
            db.WishlistItemUrls.Add(new WishlistItemUrl
            {
                Id = Guid.NewGuid(),
                WishlistItemId = item.Id,
                Url = urls[i],
                DisplayOrder = i,
            });
        }

        item.UpdatedAt = DateTime.UtcNow;
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

    private async Task FillAddedByNamesAsync(IReadOnlyCollection<WishlistItem> items, CancellationToken ct)
    {
        var ids = items.Where(i => i.AddedById is not null).Select(i => i.AddedById!.Value).Distinct().ToList();
        if (ids.Count == 0) return;

        var names = await db.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);
        foreach (var item in items)
            if (item.AddedById is { } id)
                item.AddedByName = names.GetValueOrDefault(id);
    }
}
