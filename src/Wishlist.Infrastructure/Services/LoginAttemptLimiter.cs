using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;

namespace Wishlist.Infrastructure.Services;

/// <summary>
/// Fixed-window budgets of failed password sign-ins: one per email address and one
/// per client IP, applied independently so rotating either one doesn't help. Kept in
/// memory like the other limiters: the Container App runs a single replica, and
/// someone who keeps guessing keeps that replica (and its counters) alive.
/// </summary>
public sealed class LoginAttemptLimiter : ILoginAttemptLimiter, IDisposable
{
    public const int DefaultFailuresPerEmail = 10;
    public const int DefaultFailuresPerIp = 30;
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromMinutes(15);

    private readonly PartitionedRateLimiter<string> _perEmail;
    private readonly PartitionedRateLimiter<string> _perIp;
    private readonly ILogger<LoginAttemptLimiter> _logger;

    public LoginAttemptLimiter(ILogger<LoginAttemptLimiter> logger)
        : this(logger, DefaultFailuresPerEmail, DefaultFailuresPerIp, DefaultWindow) { }

    public LoginAttemptLimiter(ILogger<LoginAttemptLimiter> logger, int failuresPerEmail, int failuresPerIp, TimeSpan window)
    {
        _logger = logger;
        _perEmail = Create(failuresPerEmail, window);
        _perIp = Create(failuresPerIp, window);
    }

    public bool IsBlocked(string email, string ipAddress) =>
        Spent(_perEmail, NormalizeEmail(email)) || Spent(_perIp, NormalizeIp(ipAddress));

    public void RecordFailure(string email, string ipAddress)
    {
        var (normalizedEmail, ip) = (NormalizeEmail(email), NormalizeIp(ipAddress));
        using (_perEmail.AttemptAcquire(normalizedEmail)) { }
        using (_perIp.AttemptAcquire(ip)) { }

        if (Spent(_perEmail, normalizedEmail) || Spent(_perIp, ip))
            _logger.LogWarning("Password sign-in limit reached (client {ClientIp}); refusing password sign-ins until the window ends.", ip);
    }

    private static PartitionedRateLimiter<string> Create(int permits, TimeSpan window) =>
        PartitionedRateLimiter.Create<string, string>(key =>
            RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permits,
                Window = window,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    private static bool Spent(PartitionedRateLimiter<string> limiter, string key) =>
        limiter.GetStatistics(key)?.CurrentAvailablePermits <= 0;

    private static string NormalizeEmail(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    private static string NormalizeIp(string ipAddress) =>
        string.IsNullOrWhiteSpace(ipAddress) ? "<unknown>" : ipAddress;

    public void Dispose()
    {
        _perEmail.Dispose();
        _perIp.Dispose();
    }
}
