namespace Wishlist.Domain.Entities;

/// <summary>A live (not struck out, not deleted) item from the old wishlist site.</summary>
public class LegacyItem
{
    /// <summary>The old site's ITEM_ID.</summary>
    public int LegacyId { get; set; }

    /// <summary>The owner's old USER_ID.</summary>
    public int LegacyOwnerId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>When the item last changed on the old site, if known.</summary>
    public DateTime? ChangedAt { get; set; }
}
