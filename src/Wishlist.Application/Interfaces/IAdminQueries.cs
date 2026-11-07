using Wishlist.Application.DTOs;

namespace Wishlist.Application.Interfaces;

/// <summary>Read-only figures for the site admin page.</summary>
public interface IAdminQueries
{
    Task<AdminTotals> GetTotalsAsync(DateTime nowUtc, CancellationToken ct = default);

    /// <summary>Creation times of accounts created at or after <paramref name="fromUtc"/>.</summary>
    Task<IReadOnlyList<DateTime>> GetSignUpTimesSinceAsync(DateTime fromUtc, CancellationToken ct = default);

    /// <summary>Distinct users with an active day from <paramref name="from"/> to <paramref name="to"/>, both included.</summary>
    Task<int> CountActiveUsersAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>The first recorded active day, or null when nothing was recorded yet.</summary>
    Task<DateOnly?> GetTrackingSinceAsync(CancellationToken ct = default);

    Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(CancellationToken ct = default);
}
