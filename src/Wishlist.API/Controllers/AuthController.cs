using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Auth;
using Wishlist.Domain.Exceptions;

namespace Wishlist.API.Controllers;

[ApiController]
[RequireHttps]
[Route("api/auth")]
public class AuthController(
    RegisterUserUseCase register,
    GetRegistrationStatusUseCase registrationStatus,
    InviteCodeGuard inviteCodeGuard,
    LoginUserUseCase login,
    RefreshTokenUseCase refresh,
    LogoutUserUseCase logout,
    RequestPasswordResetUseCase requestPasswordReset,
    ResetPasswordUseCase resetPassword,
    IForgotPasswordRateLimiter forgotPasswordRateLimiter,
    IUserRepository users) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        async Task<IActionResult> Attempt()
        {
            var (response, refreshToken) = await register.ExecuteAsync(request, ct);
            SetRefreshTokenCookie(refreshToken);
            return Ok(response);
        }

        return string.IsNullOrWhiteSpace(request.InviteCode)
            ? await Attempt()
            : await inviteCodeGuard.RunAsync(HttpContext, Attempt);
    }

    [AllowAnonymous]
    [HttpGet("registration")]
    public async Task<IActionResult> RegistrationStatus(CancellationToken ct) =>
        Ok(await registrationStatus.ExecuteAsync(ct));

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var (response, refreshToken) = await login.ExecuteAsync(request, ct);
        SetRefreshTokenCookie(refreshToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var token = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(token))
            return Unauthorized();

        try
        {
            var (accessToken, newRefreshToken) = await refresh.ExecuteAsync(token, ct);
            SetRefreshTokenCookie(newRefreshToken);
            return Ok(new { accessToken });
        }
        catch (Exception ex) when (ex is DomainException or NotFoundException)
        {
            // Expired, revoked or unknown token (or its user is gone): drop the dead cookie.
            ClearRefreshTokenCookie();
            return Unauthorized();
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var token = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(token))
            await logout.ExecuteAsync(token, ct);

        ClearRefreshTokenCookie();
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        // Reject malformed input loudly — this 400 reveals nothing about user
        // existence (we did not even attempt a lookup).
        if (string.IsNullOrWhiteSpace(request?.Email))
        {
            return BadRequest(new { error = "A valid email address is required." });
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        if (!await forgotPasswordRateLimiter.TryAcquireAsync(request.Email, ip, ct))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { error = "Too many password-reset requests. Please try again later." });
        }

        // Use case is enumeration-safe: both branches (user exists / does not)
        // complete without raising and with comparable timing.
        await requestPasswordReset.ExecuteAsync(request, ct);

        return Accepted();
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var ok = await resetPassword.ExecuteAsync(request, ct);
        if (!ok)
        {
            // Single generic error for every failure mode (bad email, expired
            // token, replayed token, weak new password) so we never leak which
            // specific check failed.
            return BadRequest(new { error = "Invalid or expired token." });
        }

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = GetUserId();
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return NotFound();
        return Ok(new UserDto(user.Id, user.Email, user.DisplayName));
    }

    [Authorize]
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        await users.UpdateDisplayNameAsync(userId, request.DisplayName, ct);
        var user = await users.GetByIdAsync(userId, ct);
        return Ok(new UserDto(user!.Id, user.Email, user.DisplayName));
    }

    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append("refreshToken", token, RefreshTokenCookieOptions(DateTimeOffset.UtcNow.AddDays(30)));
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete("refreshToken", RefreshTokenCookieOptions(expires: null));
    }

    private static CookieOptions RefreshTokenCookieOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = expires
    };

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim missing."));
}
