using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;

namespace Wishlist.Infrastructure.Services;

/// <summary>
/// Test environments only: sends email to recipients on the allow-list, with
/// <c>[TEST]</c> before the subject, and drops the rest. A dropped email is logged
/// with its subject and a masked recipient, never its body or links (they can
/// sign someone in). Production doesn't use this class at all.
/// </summary>
public sealed class AllowListEmailSender(
    IEmailSender inner,
    EmailAllowList allowList,
    ILogger<AllowListEmailSender> logger) : IEmailSender
{
    public const string SubjectPrefix = "[TEST] ";

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (!allowList.Allows(message.To))
        {
            logger.LogInformation(
                "Test environment: email \"{Subject}\" to {Recipient} not sent; the recipient isn't on Smtp:AllowedRecipients.",
                message.Subject, EmailAllowList.Mask(message.To));
            return Task.CompletedTask;
        }

        return inner.SendAsync(message with { Subject = SubjectPrefix + message.Subject }, ct);
    }
}
