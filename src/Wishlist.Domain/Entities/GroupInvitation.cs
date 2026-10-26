namespace Wishlist.Domain.Entities;

public class GroupInvitation
{
    /// <summary>How long a new invitation link works. Links already sent keep their stored expiry.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(3);

    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string InviteCode { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
