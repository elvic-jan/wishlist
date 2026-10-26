namespace Wishlist.Domain.Entities;

/// <summary>A user has dismissed (or acted on) a message; it is not shown to them again.</summary>
public class UserMessageState
{
    public Guid UserId { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public DateTime DismissedAt { get; set; }
}
