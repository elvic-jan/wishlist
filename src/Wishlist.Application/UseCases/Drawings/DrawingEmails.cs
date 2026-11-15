using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;
using Wishlist.Application.Localization;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;

namespace Wishlist.Application.UseCases.Drawings;

/// <summary>
/// The Secret Santa emails. Each goes to a participant's account address, or
/// to the address the organizer typed, in the recipient's language, else the
/// organizer's, else the request's. Best-effort: failures are logged, never thrown.
/// </summary>
public class DrawingEmails(
    IEmailSender emailSender,
    IUserRepository users,
    IAppUrlProvider appUrls,
    ILanguageContext language,
    ILogger<DrawingEmails> logger)
{
    private string Url(string path) => $"{appUrls.PublicBaseUrl.TrimEnd('/')}{path}";

    public Task SendInvitationAsync(Group group, Drawing drawing, User? organizer, DrawingParticipant to, CancellationToken ct = default) =>
        SendAsync(to, organizer, ct, (mail, name) =>
        {
            mail.Greeting(name)
                .Paragraph("{0} invited you to draw names for **{1}** on Kadoozle.",
                    OrganizerName(organizer, mail), group.Name)
                .Paragraph("Once the names are drawn, you'll get an email telling you who you drew. The drawing ends on {0}.",
                    mail.Date(drawing.EndDate));
            Budget(mail, drawing);
            var link = Url($"/join/{to.InviteCode}");
            mail.Link(link, "Join or decline");
            return mail.T("{0} invited you to draw names for {1}", OrganizerName(organizer, mail), group.Name);
        });

    /// <summary>After the draw: whose Secret Santa the giver is, and where to find that person's wishlist.</summary>
    public Task SendRecipientAsync(Group group, Drawing drawing, User? organizer, DrawingParticipant giver,
        DrawingParticipant receiver, string receiverName, CancellationToken ct = default) =>
        SendAsync(giver, organizer, ct, (mail, name) =>
        {
            mail.Greeting(name)
                .Paragraph("The names have been drawn for **{0}**. You drew **{1}**! Keep it a secret.",
                    group.Name, receiverName);
            Budget(mail, drawing);
            mail.Paragraph("The drawing ends on {0}.", mail.Date(drawing.EndDate));

            var giverJoined = giver.Status == DrawingParticipantStatus.Joined;
            var receiverJoined = receiver.Status == DrawingParticipantStatus.Joined && receiver.UserId is not null;
            var joinLink = Url($"/join/{giver.InviteCode}");
            var wishlistLink = Url($"/wishlist/{receiver.UserId}");
            var drawingLink = Url($"/drawings/{group.Id}");
            if (!giverJoined)
                mail.Link(joinLink, "Join to see {0}'s wishlist", receiverName);
            else if (receiverJoined)
                mail.Link(wishlistLink, "Open {0}'s wishlist", receiverName);
            else
                mail.Paragraph("{0} hasn't joined yet. You'll see their wishlist as soon as they have.", receiverName)
                    .Link(drawingLink, "Open the drawing");
            return mail.T("You drew {0}", receiverName);
        });

    public Task SendCancelledAsync(Group group, User? organizer, DrawingParticipant to, CancellationToken ct = default) =>
        SendAsync(to, organizer, ct, (mail, name) =>
        {
            mail.Greeting(name)
                .Paragraph("{0} cancelled the name drawing **{1}**. You don't need to do anything.",
                    OrganizerName(organizer, mail), group.Name);
            return mail.T("The name drawing {0} is cancelled", group.Name);
        });

    public Task SendBackOnAsync(Group group, User? organizer, DrawingParticipant to, CancellationToken ct = default) =>
        SendAsync(to, organizer, ct, (mail, name) =>
        {
            mail.Greeting(name)
                .Paragraph("{0} undid the cancellation of the name drawing **{1}**. Everything is as it was.",
                    OrganizerName(organizer, mail), group.Name);
            var link = Url(to.Status == DrawingParticipantStatus.Joined ? $"/drawings/{group.Id}" : $"/join/{to.InviteCode}");
            mail.Link(link, "Open the drawing");
            return mail.T("The name drawing {0} is back on", group.Name);
        });

    /// <summary>To the person who drew someone who left after the draw.</summary>
    public Task SendNoGiftNeededAsync(Group group, User? organizer, DrawingParticipant to, string leaverName, CancellationToken ct = default) =>
        SendAsync(to, organizer, ct, (mail, name) =>
        {
            mail.Greeting(name)
                .Paragraph("{0} is no longer taking part in the name drawing **{1}**, so you don't need to buy them a gift.",
                    leaverName, group.Name);
            return mail.T("No gift needed for {0}", leaverName);
        });

    /// <summary>To the organizer when someone leaves after the draw: who left, and who now has no Secret Santa.</summary>
    public async Task SendLeftAfterDrawAsync(Group group, User organizer, string leaverName, string? orphanName, CancellationToken ct = default)
    {
        var mail = new EmailText(Languages.ForEmail(organizer, null, language.Language))
            .Greeting(organizer.DisplayName)
            .Paragraph("{0} is no longer taking part in the name drawing **{1}**. The names had already been drawn.",
                leaverName, group.Name);
        if (orphanName is not null)
            mail.Paragraph("Nobody has drawn **{0}** anymore. You may want to sort out a gift for them yourself.", orphanName);
        var link = Url($"/drawings/{group.Id}");
        mail.Link(link, "Open the drawing");
        await SendAsync(mail.Build(organizer.Email, mail.T("{0} left the name drawing {1}", leaverName, group.Name)), ct);
    }

    private static void Budget(EmailText mail, Drawing drawing)
    {
        if (!string.IsNullOrWhiteSpace(drawing.Budget))
            mail.Paragraph("Budget: **{0}**", drawing.Budget);
    }

    private static string OrganizerName(User? organizer, EmailText mail) =>
        organizer?.DisplayName ?? mail.T("Someone");

    /// <summary>Builds the mail for a participant (account or typed address) and sends it.</summary>
    private async Task SendAsync(DrawingParticipant to, User? organizer, CancellationToken ct, Func<EmailText, string, string> build)
    {
        try
        {
            var account = to.UserId is { } id ? await users.GetByIdAsync(id, ct) : null;
            var address = account?.Email ?? to.Email;
            if (string.IsNullOrWhiteSpace(address)) return;

            var mail = new EmailText(Languages.ForEmail(account?.Language, organizer, language.Language));
            var subject = build(mail, account?.DisplayName ?? to.Name);
            await emailSender.SendAsync(mail.Build(address, subject), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send a drawing email to participant {ParticipantId}.", to.Id);
        }
    }

    private async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        try
        {
            await emailSender.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send a drawing email to the organizer.");
        }
    }
}
