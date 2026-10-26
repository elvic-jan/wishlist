using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Exceptions;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Repositories;

public class LegacyRepository(WishlistDbContext db) : ILegacyRepository
{
    public Task<bool> AnyAsync(CancellationToken ct = default) => db.LegacyUsers.AnyAsync(ct);

    public Task<LegacyUser?> GetByUserNameAsync(string userName, CancellationToken ct = default) =>
        db.LegacyUsers.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == userName, ct);

    public Task<LegacyUser?> GetClaimedByAsync(Guid userId, CancellationToken ct = default) =>
        db.LegacyUsers.AsNoTracking().FirstOrDefaultAsync(u => u.ClaimedByUserId == userId, ct);

    public async Task<IReadOnlyList<LegacyItem>> GetItemsAsync(int legacyOwnerId, CancellationToken ct = default) =>
        await db.LegacyItems.AsNoTracking().Where(i => i.LegacyOwnerId == legacyOwnerId).ToListAsync(ct);

    public async Task<bool> TryClaimAsync(int legacyId, Guid userId, DateTime claimedAt, int importedItemCount, CancellationToken ct = default)
    {
        try
        {
            // Conditional update: only the first of two simultaneous claims wins.
            var updated = await db.LegacyUsers
                .Where(u => u.LegacyId == legacyId && u.ClaimedByUserId == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.ClaimedByUserId, userId)
                    .SetProperty(u => u.ClaimedAt, claimedAt)
                    .SetProperty(u => u.ImportedItemCount, importedItemCount), ct);
            return updated == 1;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // The account claimed another username at the same moment.
            throw new ConflictException("You've already imported your wishes from the old site.");
        }
    }

    public async Task<(int Users, int Items)> UpsertAsync(IReadOnlyList<LegacyUser> users, IReadOnlyList<LegacyItem> items, CancellationToken ct = default)
    {
        var existingUsers = await db.LegacyUsers.ToDictionaryAsync(u => u.LegacyId, ct);
        foreach (var u in users)
        {
            if (existingUsers.TryGetValue(u.LegacyId, out var stored))
                stored.UserName = u.UserName; // claims are kept
            else
                db.LegacyUsers.Add(new LegacyUser { LegacyId = u.LegacyId, UserName = u.UserName });
        }
        await db.SaveChangesAsync(ct);

        var existingItems = await db.LegacyItems.ToDictionaryAsync(i => i.LegacyId, ct);
        foreach (var i in items)
        {
            if (existingItems.TryGetValue(i.LegacyId, out var stored))
            {
                stored.LegacyOwnerId = i.LegacyOwnerId;
                stored.Title = i.Title;
                stored.Description = i.Description;
                stored.ChangedAt = i.ChangedAt;
            }
            else
            {
                db.LegacyItems.Add(i);
            }
        }
        await db.SaveChangesAsync(ct);

        return (await db.LegacyUsers.CountAsync(ct), await db.LegacyItems.CountAsync(ct));
    }
}
