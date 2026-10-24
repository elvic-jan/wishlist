using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;

namespace Wishlist.Infrastructure.Services;

/// <summary>
/// Fixed-window budget of wrong invite codes per client IP. Kept in memory: the
/// Container App runs a single replica, and an attacker who keeps guessing keeps
/// that replica (and its counters) alive.
/// </summary>
public sealed class InviteCodeAttemptLimiter : IInviteCodeAttemptLimiter, IDisposable
{
    private const int DefaultFailuresPerWindow = 10;
    private static readonly TimeSpan DefaultWindow = TimeSpan.FromMinutes(15);

    private readonly PartitionedRateLimiter<string> _perIp;
    private readonly ILogger<InviteCodeAttemptLimiter> _logger;

    public InviteCodeAttemptLimiter(ILogger<InviteCodeAttemptLimiter> logger)
        : this(logger, DefaultFailuresPerWindow, DefaultWindow) { }

    public InviteCodeAttemptLimiter(ILogger<InviteCodeAttemptLimiter> logger, int failuresPerWindow, TimeSpan window)
    {
        _logger = logger;
        _perIp = PartitionedRateLimiter.Create<string, string>(ip =>
            RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = failuresPerWindow,
                Window = window,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    }

    public bool IsBlocked(string ipAddress) =>
        _perIp.GetStatistics(Normalize(ipAddress))?.CurrentAvailablePermits <= 0;

    public void RecordFailure(string ipAddress)
    {
        var ip = Normalize(ipAddress);
        using var lease = _perIp.AttemptAcquire(ip);
        if (lease.IsAcquired && IsBlocked(ip))
        {
            // Logged once per window: later attempts are refused before they
            // reach this point.
            _logger.LogWarning("Invite-code guessing limit reached for {ClientIp}; blocking further attempts.", ip);
        }
    }

    private static string Normalize(string ipAddress) =>
        string.IsNullOrWhiteSpace(ipAddress) ? "<unknown>" : ipAddress;

    public void Dispose() => _perIp.Dispose();
}
