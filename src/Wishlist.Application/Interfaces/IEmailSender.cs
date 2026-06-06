namespace Wishlist.Application.Interfaces;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public record EmailMessage(
    string To,
    string Subject,
    string PlainTextBody,
    string? HtmlBody = null);
