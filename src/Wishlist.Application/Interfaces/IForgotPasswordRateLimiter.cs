namespace Wishlist.Application.Interfaces;

/// <summary>
/// Enforces per-email and per-IP rate limits for password-reset requests,
/// independently of any specific endpoint plumbing. A request is permitted
/// only if BOTH limits have remaining capacity for the current window.
/// </summary>
public interface IForgotPasswordRateLimiter
{
    ValueTask<bool> TryAcquireAsync(string email, string ipAddress, CancellationToken ct = default);
}
