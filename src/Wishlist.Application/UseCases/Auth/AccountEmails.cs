using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;
using Wishlist.Application.Localization;
using Wishlist.Domain.Entities;

namespace Wishlist.Application.UseCases.Auth;

/// <summary>
/// Account emails that are sent best-effort: a delivery failure is logged and
/// never fails the operation that triggered it. They go to the person acting,
/// so without a saved language they use the language of the request.
/// </summary>
public class AccountEmails(
    IUserRepository users,
    IEmailSender emailSender,
    IAppUrlProvider appUrls,
    ILanguageContext language,
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
        var mail = Mail(user)
            .Greeting(user.DisplayName)
            .Paragraph(
                "Welcome to Kadoozle! Please confirm this is your email address, so you can " +
                "get back into your account if you ever lose your phone or forget how you signed in.")
            .Link(link, "Confirm my email")
            .Paragraph("The link works for 3 days. If you didn't create a Kadoozle account, you can ignore this email.");

        await SendAsync(user, mail.Build(user.Email, mail.T("Confirm your Kadoozle email")), "email-confirmation", ct);
    }

    public Task SendPasskeyAddedAsync(User user, string? passkeyName, CancellationToken ct = default)
    {
        var at = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
        var settings = $"{BaseUrl}/settings/security";
        var mail = Mail(user).Greeting(user.DisplayName);
        if (string.IsNullOrWhiteSpace(passkeyName))
            mail.Paragraph(
                "A new passkey was just added to your Kadoozle account (at **{0}**). " +
                "It lets you sign in with Face ID, a fingerprint or your device PIN.", at);
        else
            mail.Paragraph(
                "A new passkey (\"{0}\") was just added to your Kadoozle account (at **{1}**). " +
                "It lets you sign in with Face ID, a fingerprint or your device PIN.", passkeyName, at);
        mail.Paragraph("If this was you, there's nothing to do.")
            .Link(settings,
                "If it wasn't you, open your sign-in settings, remove the passkey and sign out other devices");

        return SendAsync(user, mail.Build(user.Email, mail.T("A passkey was added to your Kadoozle account")), "passkey-added", ct);
    }

    private EmailText Mail(User user) => new(Languages.ForEmail(user, null, language.Language));

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
