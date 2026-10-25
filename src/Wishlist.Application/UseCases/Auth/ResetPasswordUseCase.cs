using Microsoft.Extensions.Logging;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;

namespace Wishlist.Application.UseCases.Auth;

/// <summary>
/// Redeems a password-reset token and sets a new password. On success, all
/// existing refresh tokens for the user are revoked and a confirmation email
/// is dispatched best-effort. Returns <c>false</c> for any failure path,
/// without distinguishing them, so the caller can respond with a single
/// generic error message.
/// </summary>
public class ResetPasswordUseCase(
    IUserRepository users,
    ITokenService tokens,
    IEmailSender emailSender,
    ILogger<ResetPasswordUseCase> logger)
{
    public async Task<bool> ExecuteAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Token)
            || string.IsNullOrEmpty(request.NewPassword)
            || request.NewPassword.Length < 8)
        {
            return false;
        }

        var user = await users.GetByEmailAsync(request.Email, ct);
        if (user is null)
        {
            // Do not leak existence: same failure shape as a bad token.
            return false;
        }

        var ok = await users.ResetPasswordAsync(user.Id, request.Token, request.NewPassword, ct);
        if (!ok)
        {
            return false;
        }

        // Kick every existing session: the user has just signaled "my old
        // credentials may be compromised."
        await tokens.RevokeAllUserRefreshTokensAsync(user.Id, ct);

        // The token reached them by email, so the address is theirs.
        await users.MarkEmailConfirmedAsync(user.Id, ct);

        // Out-of-band confirmation so the legitimate inbox owner has a chance
        // to react if the reset was triggered by someone else. Best-effort —
        // a delivery failure here MUST NOT fail the reset.
        var changedAtUtc = DateTime.UtcNow;
        var message = new EmailMessage(
            To: user.Email,
            Subject: "Your GiftCircles password was changed",
            PlainTextBody:
                $"Hi {user.DisplayName},\r\n\r\n" +
                $"Your GiftCircles account password was just changed (at {changedAtUtc:yyyy-MM-dd HH:mm} UTC).\r\n\r\n" +
                "If you made this change, no action is needed.\r\n\r\n" +
                "If you did NOT make this change, request another password reset from the " +
                "sign-in page right away, and contact support if you cannot regain access.\r\n\r\n" +
                "Thanks,\r\nGiftCircles",
            HtmlBody:
                $"<p>Hi {System.Net.WebUtility.HtmlEncode(user.DisplayName)},</p>" +
                $"<p>Your GiftCircles account password was just changed (at <strong>{changedAtUtc:yyyy-MM-dd HH:mm} UTC</strong>).</p>" +
                "<p>If you made this change, no action is needed.</p>" +
                "<p>If you did <strong>not</strong> make this change, request another password reset from the " +
                "sign-in page right away, and contact support if you cannot regain access.</p>" +
                "<p>Thanks,<br/>GiftCircles</p>");

        try
        {
            await emailSender.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Password reset for user {UserId} succeeded but confirmation email failed.",
                user.Id);
        }

        return true;
    }
}
