using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;
using Wishlist.Application.Localization;
using Wishlist.Domain.Entities;

namespace Wishlist.Application.UseCases.Buddies;

/// <summary>Tells someone they've been made a buddy for another person's wishlist.</summary>
public class BuddyEmails(IEmailSender emailSender, IAppUrlProvider appUrls, ILanguageContext language, ILogger<BuddyEmails> logger)
{
    /// <summary>Best-effort: a failure is logged, never thrown.</summary>
    public async Task SendNewBuddyAsync(User buddy, User owner, CancellationToken ct = default)
    {
        var link = $"{appUrls.PublicBaseUrl.TrimEnd('/')}/wishlist/{owner.Id}";
        var mail = new EmailText(Languages.ForEmail(buddy, owner, language.Language))
            .Greeting(buddy.DisplayName)
            .Paragraph(
                "{0} made you a **buddy** for their wishlist on Kadoozle. " +
                "You can now add, change and remove wishes on their list, for example when you know what they'd like.",
                owner.DisplayName)
            .Link(link, "Open {0}'s wishlist", owner.DisplayName);
        var message = mail.Build(buddy.Email, mail.T("{0} made you a buddy for their wishlist", owner.DisplayName));

        try
        {
            await emailSender.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send the new-buddy email to user {UserId}.", buddy.Id);
        }
    }
}
