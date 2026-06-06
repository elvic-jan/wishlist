using System.Threading.RateLimiting;
using Wishlist.Application.Interfaces;

namespace Wishlist.Infrastructure.Services;

/// <summary>
/// Combines two independent partitioned rate limits — per email and per
/// remote IP — and grants a request only when BOTH have remaining capacity.
/// Limits are applied independently (not as a tuple) so an attacker cannot
/// circumvent the per-email limit by rotating IPs, nor circumvent the per-IP
/// limit by rotating target emails.
/// </summary>
public sealed class ForgotPasswordRateLimiter : IForgotPasswordRateLimiter, IDisposable
{
    private const int PerEmailPermits = 5;
    private const int PerIpPermits = 20;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private readonly PartitionedRateLimiter<string> _perEmail;
    private readonly PartitionedRateLimiter<string> _perIp;

    public ForgotPasswordRateLimiter()
    {
        _perEmail = PartitionedRateLimiter.Create<string, string>(email =>
            RateLimitPartition.GetFixedWindowLimiter(email, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = PerEmailPermits,
                Window = Window,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

        _perIp = PartitionedRateLimiter.Create<string, string>(ip =>
            RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = PerIpPermits,
                Window = Window,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    }

    public async ValueTask<bool> TryAcquireAsync(string email, string ipAddress, CancellationToken ct = default)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedIp = string.IsNullOrWhiteSpace(ipAddress) ? "<unknown>" : ipAddress;

        using var emailLease = await _perEmail.AcquireAsync(normalizedEmail, permitCount: 1, ct);
        if (!emailLease.IsAcquired)
        {
            return false;
        }

        using var ipLease = await _perIp.AcquireAsync(normalizedIp, permitCount: 1, ct);
        return ipLease.IsAcquired;
    }

    public void Dispose()
    {
        _perEmail.Dispose();
        _perIp.Dispose();
    }
}
