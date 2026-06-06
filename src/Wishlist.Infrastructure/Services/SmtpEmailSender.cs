using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Wishlist.Application.Interfaces;
using Wishlist.Infrastructure.Options;

namespace Wishlist.Infrastructure.Services;

/// <summary>
/// Sends mail via MailKit's <see cref="SmtpClient"/> using
/// <see cref="SmtpOptions"/>. The whole send is wrapped in a 5-second timeout
/// — exceeding it logs a warning and returns without throwing, so callers
/// (e.g. a forgot-password endpoint that must return 202 regardless) are not
/// blocked by a slow or unreachable SMTP server.
/// </summary>
public class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(5);

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var opts = options.Value;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(opts.FromName, opts.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;

        if (!string.IsNullOrEmpty(message.HtmlBody))
        {
            var multipart = new Multipart("alternative");
            multipart.Add(new TextPart(TextFormat.Plain) { Text = message.PlainTextBody });
            multipart.Add(new TextPart(TextFormat.Html) { Text = message.HtmlBody });
            mime.Body = multipart;
        }
        else
        {
            mime.Body = new TextPart(TextFormat.Plain) { Text = message.PlainTextBody };
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(SendTimeout);

        try
        {
            using var client = new SmtpClient();

            var socketOptions = opts.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(opts.Host, opts.Port, socketOptions, timeoutCts.Token);

            if (!string.IsNullOrEmpty(opts.User))
            {
                await client.AuthenticateAsync(opts.User, opts.Password ?? string.Empty, timeoutCts.Token);
            }

            await client.SendAsync(mime, timeoutCts.Token);
            await client.DisconnectAsync(quit: true, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(
                "SMTP send to {To} timed out after {Timeout}; giving up without throwing.",
                message.To, SendTimeout);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SMTP send to {To} failed.", message.To);
        }
    }
}
