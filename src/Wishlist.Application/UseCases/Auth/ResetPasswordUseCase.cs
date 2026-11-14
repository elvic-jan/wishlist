using Microsoft.Extensions.Logging;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Application.Localization;

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
    ILanguageContext language,
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
        InputLimits.Check(request.NewPassword, InputLimits.Password, "Password");

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
        var mail = new EmailText(Languages.ForEmail(user, null, language.Language))
            .Greeting(user.DisplayName)
            .Paragraph("Your Kadoozle account password was just changed (at **{0}**).", $"{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
            .Paragraph("If you made this change, no action is needed.")
            .Paragraph(
                "If you did **not** make this change, request another password reset from the " +
                "sign-in page right away, and contact support if you cannot regain access.");
        var message = mail.Build(user.Email, mail.T("Your Kadoozle password was changed"));

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
