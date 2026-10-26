using System.Threading.RateLimiting;
using Wishlist.Application.Interfaces;

namespace Wishlist.Infrastructure.Services;

/// <summary>
/// Fixed-window budget of unknown old usernames per account (10 per hour). In
/// memory, like the invite-code limiter: the app runs a single replica.
/// </summary>
public sealed class LegacyUserNameAttemptLimiter : ILegacyUserNameAttemptLimiter, IDisposable
{
    public const int FailuresPerWindow = 10;
    private static readonly TimeSpan Window = TimeSpan.FromHours(1);

    private readonly PartitionedRateLimiter<Guid> _perUser = PartitionedRateLimiter.Create<Guid, Guid>(id =>
        RateLimitPartition.GetFixedWindowLimiter(id, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = FailuresPerWindow,
            Window = Window,
            QueueLimit = 0,
            AutoReplenishment = true,
        }));

    public bool IsBlocked(Guid userId) => _perUser.GetStatistics(userId)?.CurrentAvailablePermits <= 0;

    public void RecordFailure(Guid userId)
    {
        using var _ = _perUser.AttemptAcquire(userId);
    }

    public void Dispose() => _perUser.Dispose();
}
