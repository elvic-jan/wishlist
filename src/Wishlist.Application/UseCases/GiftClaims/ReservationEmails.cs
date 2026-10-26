using System.Globalization;
using System.Net;
using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;

namespace Wishlist.Application.UseCases.GiftClaims;

/// <summary>
/// Emails to the person who reserved a gift. They go only to the reserver and
/// never mention other givers, so nothing reaches the wishlist's owner.
/// </summary>
public class ReservationEmails(IEmailSender emailSender, IAppUrlProvider appUrls, ILogger<ReservationEmails> logger)
{
    /// <summary>Returns whether the email was handed to the email service.</summary>
    public Task<bool> SendReminderAsync(User reserver, User owner, ReservationNotice notice, CancellationToken ct = default)
    {
        var link = WishlistLink(owner);
        var until = FormatDate(notice.ExpiresAt);
        var item = notice.ItemTitle;

        return SendAsync(reserver, new EmailMessage(
            To: reserver.Email,
            Subject: $"Your reservation of \"{item}\" ends on {until}",
            PlainTextBody:
                $"Hi {reserver.DisplayName},\r\n\r\n" +
                $"A while ago you reserved \"{item}\" on {owner.DisplayName}'s wishlist. " +
                $"The reservation is still open and ends on {until}. After that, others can reserve it again.\r\n\r\n" +
                "Open the wishlist to keep your reservation, mark the gift as bought, or release it:\r\n" +
                $"{link}\r\n\r\n" +
                "Thanks,\r\nGiftCircles",
            HtmlBody:
                $"<p>Hi {Html(reserver.DisplayName)},</p>" +
                $"<p>A while ago you reserved <strong>{Html(item)}</strong> on {Html(owner.DisplayName)}&rsquo;s wishlist. " +
                $"The reservation is still open and ends on <strong>{until}</strong>. After that, others can reserve it again.</p>" +
                "<p>Open the wishlist to keep your reservation, mark the gift as bought, or release it:</p>" +
                $"<p><a href=\"{link}\">Open {Html(owner.DisplayName)}&rsquo;s wishlist</a></p>" +
                "<p>Thanks,<br/>GiftCircles</p>"), "reservation-reminder", ct);
    }

    /// <summary>Returns whether the email was handed to the email service.</summary>
    public Task<bool> SendExpiredAsync(User reserver, User owner, ReservationNotice notice, CancellationToken ct = default)
    {
        var link = WishlistLink(owner);
        var item = notice.ItemTitle;

        return SendAsync(reserver, new EmailMessage(
            To: reserver.Email,
            Subject: $"Your reservation of \"{item}\" has ended",
            PlainTextBody:
                $"Hi {reserver.DisplayName},\r\n\r\n" +
                $"Your reservation of \"{item}\" on {owner.DisplayName}'s wishlist has ended, " +
                "so others in the circle can reserve it now.\r\n\r\n" +
                "If you still want to give it, open the wishlist and reserve it again while it's free:\r\n" +
                $"{link}\r\n\r\n" +
                "Thanks,\r\nGiftCircles",
            HtmlBody:
                $"<p>Hi {Html(reserver.DisplayName)},</p>" +
                $"<p>Your reservation of <strong>{Html(item)}</strong> on {Html(owner.DisplayName)}&rsquo;s wishlist has ended, " +
                "so others in the circle can reserve it now.</p>" +
                "<p>If you still want to give it, open the wishlist and reserve it again while it&rsquo;s free:</p>" +
                $"<p><a href=\"{link}\">Open {Html(owner.DisplayName)}&rsquo;s wishlist</a></p>" +
                "<p>Thanks,<br/>GiftCircles</p>"), "reservation-expired", ct);
    }

    private string WishlistLink(User owner) => $"{appUrls.PublicBaseUrl.TrimEnd('/')}/wishlist/{owner.Id}";

    private static string FormatDate(DateTime utc) => utc.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

    private static string Html(string text) => WebUtility.HtmlEncode(text);

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
