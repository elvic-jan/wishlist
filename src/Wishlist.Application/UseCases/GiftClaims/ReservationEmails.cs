using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;
using Wishlist.Application.Localization;
using Wishlist.Domain.Entities;

namespace Wishlist.Application.UseCases.GiftClaims;

/// <summary>
/// Emails to the person who reserved a gift. They go only to the reserver and
/// never mention other givers, so nothing reaches the wishlist's owner. Sent
/// by the monthly run, so in the reserver's saved language, else English.
/// </summary>
public class ReservationEmails(IEmailSender emailSender, IAppUrlProvider appUrls, ILanguageContext language, ILogger<ReservationEmails> logger)
{
    /// <summary>Returns whether the email was handed to the email service.</summary>
    public Task<bool> SendReminderAsync(User reserver, User owner, ReservationNotice notice, CancellationToken ct = default)
    {
        var mail = Mail(reserver);
        var until = mail.Date(notice.ExpiresAt);
        var item = notice.ItemTitle;

        mail.Greeting(reserver.DisplayName)
            .Paragraph(
                "A while ago you reserved **{0}** on {1}'s wishlist. " +
                "The reservation is still open and ends on **{2}**. After that, others can reserve it again.",
                item, owner.DisplayName, until)
            .Paragraph("Open the wishlist to keep your reservation, mark the gift as bought, or release it.")
            .Link(WishlistLink(owner), "Open {0}'s wishlist", owner.DisplayName);

        return SendAsync(reserver,
            mail.Build(reserver.Email, mail.T("Your reservation of \"{0}\" ends on {1}", item, until)),
            "reservation-reminder", ct);
    }

    /// <summary>Returns whether the email was handed to the email service.</summary>
    public Task<bool> SendExpiredAsync(User reserver, User owner, ReservationNotice notice, CancellationToken ct = default)
    {
        var item = notice.ItemTitle;
        var mail = Mail(reserver)
            .Greeting(reserver.DisplayName)
            .Paragraph(
                "Your reservation of **{0}** on {1}'s wishlist has ended, so others in the circle can reserve it now.",
                item, owner.DisplayName)
            .Paragraph("If you still want to give it, open the wishlist and reserve it again while it's free.")
            .Link(WishlistLink(owner), "Open {0}'s wishlist", owner.DisplayName);

        return SendAsync(reserver,
            mail.Build(reserver.Email, mail.T("Your reservation of \"{0}\" has ended", item)),
            "reservation-expired", ct);
    }

    private EmailText Mail(User reserver) => new(Languages.ForEmail(reserver, null, language.Language));

    private string WishlistLink(User owner) => $"{appUrls.PublicBaseUrl.TrimEnd('/')}/wishlist/{owner.Id}";

    private async Task<bool> SendAsync(User reserver, EmailMessage message, string kind, CancellationToken ct)
    {
        try
        {
            await emailSender.SendAsync(message, ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send the {Kind} email to user {UserId}.", kind, reserver.Id);
            return false;
        }
    }
}
