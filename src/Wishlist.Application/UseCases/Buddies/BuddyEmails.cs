using System.Net;
using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;

namespace Wishlist.Application.UseCases.Buddies;

/// <summary>Tells someone they've been made a buddy for another person's wishlist.</summary>
public class BuddyEmails(IEmailSender emailSender, IAppUrlProvider appUrls, ILogger<BuddyEmails> logger)
{
    /// <summary>Best-effort: a failure is logged, never thrown.</summary>
    public async Task SendNewBuddyAsync(User buddy, User owner, CancellationToken ct = default)
    {
        var link = $"{appUrls.PublicBaseUrl.TrimEnd('/')}/wishlist/{owner.Id}";
        var message = new EmailMessage(
            To: buddy.Email,
            Subject: $"{owner.DisplayName} made you a buddy for their wishlist",
            PlainTextBody:
                $"Hi {buddy.DisplayName},\r\n\r\n" +
                $"{owner.DisplayName} made you a buddy for their wishlist on GiftCircles. " +
                "You can now add, change and remove wishes on their list, for example when you know what they'd like.\r\n\r\n" +
                $"Open {owner.DisplayName}'s wishlist:\r\n" +
                $"{link}\r\n\r\n" +
                "Thanks,\r\nGiftCircles",
            HtmlBody:
                $"<p>Hi {Html(buddy.DisplayName)},</p>" +
                $"<p>{Html(owner.DisplayName)} made you a <strong>buddy</strong> for their wishlist on GiftCircles. " +
                "You can now add, change and remove wishes on their list, for example when you know what they&rsquo;d like.</p>" +
                $"<p><a href=\"{link}\">Open {Html(owner.DisplayName)}&rsquo;s wishlist</a></p>" +
                "<p>Thanks,<br/>GiftCircles</p>");

        try
        {
            await emailSender.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send the new-buddy email to user {UserId}.", buddy.Id);
        }
    }

    private static string Html(string text) => WebUtility.HtmlEncode(text);
}
