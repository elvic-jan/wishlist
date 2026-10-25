using System.Net;
using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;

namespace Wishlist.Application.UseCases.Auth;

/// <summary>
/// Account emails that are sent best-effort: a delivery failure is logged and
/// never fails the operation that triggered it.
/// </summary>
public class AccountEmails(
    IUserRepository users,
    IEmailSender emailSender,
    IAppUrlProvider appUrls,
    ILogger<AccountEmails> logger)
{
    public async Task SendEmailConfirmationAsync(User user, CancellationToken ct = default)
    {
        string token;
        try
        {
            token = await users.GenerateEmailConfirmationTokenAsync(user.Id, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate an email-confirmation token for user {UserId}.", user.Id);
            return;
        }

        var link = $"{BaseUrl}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";
        await SendAsync(user, new EmailMessage(
            To: user.Email,
            Subject: "Confirm your GiftCircles email",
            PlainTextBody:
                $"Hi {user.DisplayName},\r\n\r\n" +
                "Welcome to GiftCircles! Please confirm this is your email address, so you can " +
                "get back into your account if you ever lose your phone or forget how you signed in.\r\n\r\n" +
                $"{link}\r\n\r\n" +
                "The link works for 3 days. If you didn't create a GiftCircles account, you can ignore this email.\r\n\r\n" +
                "Thanks,\r\nGiftCircles",
            HtmlBody:
                $"<p>Hi {WebUtility.HtmlEncode(user.DisplayName)},</p>" +
                "<p>Welcome to GiftCircles! Please confirm this is your email address, so you can " +
                "get back into your account if you ever lose your phone or forget how you signed in.</p>" +
                $"<p><a href=\"{link}\">Confirm my email</a></p>" +
                "<p>The link works for 3 days. If you didn't create a GiftCircles account, you can ignore this email.</p>" +
                "<p>Thanks,<br/>GiftCircles</p>"), "email-confirmation", ct);
    }

    public Task SendPasskeyAddedAsync(User user, string? passkeyName, CancellationToken ct = default)
    {
        var what = string.IsNullOrWhiteSpace(passkeyName) ? "A new passkey" : $"A new passkey (\"{passkeyName}\")";
        var whatHtml = string.IsNullOrWhiteSpace(passkeyName)
            ? "A new passkey"
            : $"A new passkey (&ldquo;{WebUtility.HtmlEncode(passkeyName)}&rdquo;)";
        var settings = $"{BaseUrl}/settings/security";

        return SendAsync(user, new EmailMessage(
            To: user.Email,
            Subject: "A passkey was added to your GiftCircles account",
            PlainTextBody:
                $"Hi {user.DisplayName},\r\n\r\n" +
                $"{what} was just added to your GiftCircles account (at {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC). " +
                "It lets you sign in with Face ID, a fingerprint or your device PIN.\r\n\r\n" +
                "If this was you, there's nothing to do.\r\n\r\n" +
                "If it wasn't you, open your sign-in settings, remove the passkey and sign out other devices:\r\n" +
                $"{settings}\r\n\r\n" +
                "Thanks,\r\nGiftCircles",
            HtmlBody:
                $"<p>Hi {WebUtility.HtmlEncode(user.DisplayName)},</p>" +
                $"<p>{whatHtml} was just added to your GiftCircles account " +
                $"(at <strong>{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</strong>). " +
                "It lets you sign in with Face ID, a fingerprint or your device PIN.</p>" +
                "<p>If this was you, there's nothing to do.</p>" +
                "<p>If it wasn't you, open your sign-in settings, remove the passkey and sign out other devices: " +
                $"<a href=\"{settings}\">{settings}</a></p>" +
                "<p>Thanks,<br/>GiftCircles</p>"), "passkey-added", ct);
    }

    private string BaseUrl => appUrls.PublicBaseUrl.TrimEnd('/');

    private async Task SendAsync(User user, EmailMessage message, string kind, CancellationToken ct)
    {
        try
        {
            await emailSender.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send the {Kind} email to user {UserId}.", kind, user.Id);
        }
    }
}
