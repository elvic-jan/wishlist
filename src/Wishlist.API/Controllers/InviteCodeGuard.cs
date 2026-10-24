using Microsoft.AspNetCore.Mvc;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.API.Controllers;

/// <summary>
/// Shared by every endpoint that accepts an invite code (register, join) so they
/// draw on one wrong-guess budget per client IP. A blocked IP is refused before
/// its code is looked at, so even a correct guess is never confirmed.
/// </summary>
public sealed class InviteCodeGuard(IInviteCodeAttemptLimiter limiter)
{
    public async Task<IActionResult> RunAsync(HttpContext http, Func<Task<IActionResult>> attempt)
    {
        var ip = http.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        if (limiter.IsBlocked(ip))
        {
            return new ObjectResult(new { error = "Too many invitation attempts. Please try again later." })
            {
                StatusCode = StatusCodes.Status429TooManyRequests,
            };
        }

        try
        {
            return await attempt();
        }
        catch (InvitationNotFoundException)
        {
            // Only unknown codes count; expired ones were genuinely handed out.
            limiter.RecordFailure(ip);
            throw;
        }
    }
}
