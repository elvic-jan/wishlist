using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.Messages;

public record MessageAction(string Label, string Url);

/// <summary>
/// A message shown to users as a lasting toast until they act on it or dismiss
/// it. Add one by writing a class and registering it in DependencyInjection;
/// the id must never change, because dismissals are stored against it.
/// </summary>
public interface IMessageDefinition
{
    string Id { get; }
    string Title { get; }
    string Text { get; }
    MessageAction? Action { get; }

    /// <summary>Shown from this moment (UTC); null = no start.</summary>
    DateTime? ShowFrom { get; }

    /// <summary>Shown until this moment (UTC); null = no end.</summary>
    DateTime? ShowUntil { get; }

    /// <summary>Whether the message is meant for this user.</summary>
    Task<bool> AppliesToAsync(Guid userId, CancellationToken ct = default);
}

public record MessageDto(string Id, string Title, string Text, MessageAction? Action);

/// <summary>The messages for a user: in their date window, meant for them, not dismissed. In registration order.</summary>
public class GetMyMessagesUseCase(IEnumerable<IMessageDefinition> definitions, IUserMessageStateRepository states, TimeProvider time)
{
    public async Task<IReadOnlyList<MessageDto>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var dismissed = await states.GetDismissedAsync(userId, ct);
        var result = new List<MessageDto>();
        foreach (var m in definitions)
        {
            if (dismissed.Contains(m.Id)) continue;
            if (m.ShowFrom is { } from && now < from) continue;
            if (m.ShowUntil is { } until && now >= until) continue;
            if (!await m.AppliesToAsync(userId, ct)) continue;
            result.Add(new MessageDto(m.Id, m.Title, m.Text, m.Action));
        }
        return result;
    }
}

/// <summary>Hides a message from the user for good (also used when they take its action).</summary>
public class DismissMessageUseCase(IEnumerable<IMessageDefinition> definitions, IUserMessageStateRepository states, TimeProvider time)
{
    public async Task ExecuteAsync(Guid userId, string messageId, CancellationToken ct = default)
    {
        if (!definitions.Any(m => m.Id == messageId))
            throw new NotFoundException("Message not found.");

        await states.DismissAsync(userId, messageId, time.GetUtcNow().UtcDateTime, ct);
    }
}

/// <summary>Invites people who haven't imported to bring their wishes from the old site, once its data is loaded.</summary>
public class LegacyImportMessage(ILegacyRepository legacy) : IMessageDefinition
{
    public string Id => "legacy-import";
    public string Title => "Bring your wishes from the old site";
    public string Text => "Had a wishlist on verlanglijstje? Import your wishes into GiftCircles in a few taps, so you don't have to type them again.";
    public MessageAction? Action => new("Import my wishes", "/import");
    public DateTime? ShowFrom => null;
    public DateTime? ShowUntil => null;

    public async Task<bool> AppliesToAsync(Guid userId, CancellationToken ct = default) =>
        await legacy.AnyAsync(ct) && await legacy.GetClaimedByAsync(userId, ct) is null;
}

public class WhatsNewNovember2026Message : IMessageDefinition
{
    public string Id => "whats-new-2026-11";
    public string Title => "What's new";
    public string Text =>
        "Invite people straight to WhatsApp or by email with \"Invite people\" on a circle. " +
        "And if you reserve a gift, we'll remind you before the reservation runs out.";
    public MessageAction? Action => null;
    public DateTime? ShowFrom => null;
    public DateTime? ShowUntil => new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public Task<bool> AppliesToAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(true);
}
