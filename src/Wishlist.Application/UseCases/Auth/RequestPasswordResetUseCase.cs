using Microsoft.Extensions.Logging;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;

namespace Wishlist.Application.UseCases.Auth;

/// <summary>
/// Handles a forgot-password request. MUST be enumeration-safe: the caller of
/// this use case cannot tell from the outside whether the email matched a
/// registered user — both branches return without raising, and both branches
/// do roughly the same amount of work.
/// </summary>
public class RequestPasswordResetUseCase(
    IUserRepository users,
    IEmailSender emailSender,
    IAppUrlProvider appUrls,
    ILogger<RequestPasswordResetUseCase> logger)
{
    // Constant-time-ish delay for the "no user" branch so response timing
    // does not leak the existence of an account. Calibrated to roughly match
    // the cost of UserManager.GeneratePasswordResetTokenAsync.
    private static readonly TimeSpan NoUserDelay = TimeSpan.FromMilliseconds(50);

    public async Task ExecuteAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByEmailAsync(request.Email, ct);

        // Recovery mail only goes to an address the account owner has proved, or
        // to accounts with a password (which have always had reset by email). A
        // passkey-only account with an unconfirmed address — say, a typo at
        // sign-up — must not let a stranger's inbox take it over.
        if (user is null || (!user.EmailConfirmed && !user.HasPassword))
        {
            await Task.Delay(NoUserDelay, ct);
            return;
        }

        string token;
        try
        {
            token = await users.GeneratePasswordResetTokenAsync(user.Id, ct);
        }
        catch (Exception ex)
        {
            // Never propagate — that would leak existence via 500 vs 202.
            logger.LogWarning(ex, "Failed to generate password-reset token for user {UserId}.", user.Id);
            return;
        }

        var resetUrl = RecoveryLink.Build(appUrls.PublicBaseUrl, user.Email, token);

        var message = new EmailMessage(
            To: user.Email,
            Subject: "Get back into your Kadoozle account",
            PlainTextBody:
                $"Hi {user.DisplayName},\r\n\r\n" +
                "We received a request to get back into your Kadoozle account. " +
                "If you did not make this request, you can safely ignore this email.\r\n\r\n" +
                "Open the link below within the next 30 minutes, on the phone or computer you want to use. " +
                "You can then sign in with Face ID or a fingerprint on that device, or set a new password:\r\n" +
                $"{resetUrl}\r\n\r\n" +
                "Thanks,\r\nKadoozle",
            HtmlBody:
                $"<p>Hi {System.Net.WebUtility.HtmlEncode(user.DisplayName)},</p>" +
                "<p>We received a request to get back into your Kadoozle account. " +
                "If you did not make this request, you can safely ignore this email.</p>" +
                "<p>Open the link below within the next 30 minutes, on the phone or computer you want to use. " +
                "You can then sign in with Face ID or a fingerprint on that device, or set a new password:</p>" +
                $"<p><a href=\"{resetUrl}\">{resetUrl}</a></p>" +
                "<p>Thanks,<br/>Kadoozle</p>");

        try
        {
            await emailSender.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            // Same reason: failure to deliver must not turn into a 500.
            logger.LogWarning(ex, "Failed to send password-reset email to user {UserId}.", user.Id);
        }
    }
}
