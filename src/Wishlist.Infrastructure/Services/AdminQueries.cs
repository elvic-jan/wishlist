using Microsoft.EntityFrameworkCore;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain;
using Wishlist.Domain.Enums;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Services;

public class AdminQueries(WishlistDbContext db) : IAdminQueries
{
    public async Task<AdminTotals> GetTotalsAsync(DateTime nowUtc, CancellationToken ct = default) => new(
        await db.Users.CountAsync(ct),
        await db.Groups.CountAsync(ct),
        await db.WishlistItems.CountAsync(ct),
        await db.GiftClaims.CountAsync(
            c => c.Status == GiftClaimStatus.Reserved && (c.ExpiresAt == null || c.ExpiresAt > nowUtc), ct));

    public async Task<IReadOnlyList<DateTime>> GetSignUpTimesSinceAsync(DateTime fromUtc, CancellationToken ct = default) =>
        await db.Users.Where(u => u.CreatedAt >= fromUtc).Select(u => u.CreatedAt).ToListAsync(ct);

    public Task<int> CountActiveUsersAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        db.UserActivityDays.Where(a => a.Day >= from && a.Day <= to).Select(a => a.UserId).Distinct().CountAsync(ct);

    public Task<DateOnly?> GetTrackingSinceAsync(CancellationToken ct = default) =>
        db.UserActivityDays.Select(a => (DateOnly?)a.Day).MinAsync(ct);

    public async Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(CancellationToken ct = default)
    {
        // One grouped query per figure, joined in memory: the user base is small.
        var users = await db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.DisplayName, u.Email, u.CreatedAt, u.EmailConfirmed, HasPassword = u.PasswordHash != null })
            .ToListAsync(ct);
        var passkeys = (await db.UserPasskeys.Select(p => p.UserId).Distinct().ToListAsync(ct)).ToHashSet();
        var admins = (await (from ur in db.UserRoles
                             join r in db.Roles on ur.RoleId equals r.Id
                             where r.Name == SiteRoles.Admin
                             select ur.UserId).ToListAsync(ct)).ToHashSet();
        var circles = await db.GroupMembers.GroupBy(m => m.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);
        var signIns = await db.SignInEvents.GroupBy(s => s.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count(), Last = g.Max(s => s.OccurredAt) })
            .ToDictionaryAsync(x => x.UserId, ct);
        var lastActive = await db.UserActivityDays.GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, Last = g.Max(a => a.Day) })
            .ToDictionaryAsync(x => x.UserId, x => x.Last, ct);

        return users
            .OrderByDescending(u => u.CreatedAt)
            .Select(u =>
            {
                var s = signIns.GetValueOrDefault(u.Id);
                return new AdminUserDto(
                    u.Id, u.DisplayName, u.Email ?? string.Empty, u.CreatedAt, u.EmailConfirmed, u.HasPassword,
                    passkeys.Contains(u.Id), admins.Contains(u.Id), circles.GetValueOrDefault(u.Id),
                    s?.Last, s?.Count ?? 0,
                    lastActive.TryGetValue(u.Id, out var day) ? day : null);
            })
            .ToList();
    }
}
