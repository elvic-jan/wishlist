namespace Wishlist.Domain.Entities;

public class WishlistItem
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }

    /// <summary>The buddy who added this wish; null when the owner added it.</summary>
    public Guid? AddedById { get; set; }

    /// <summary>Display name of <see cref="AddedById"/>; filled by the repository, not stored.</summary>
    public string? AddedByName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public IList<WishlistItemUrl> Urls { get; set; } = new List<WishlistItemUrl>();

    public GiftClaim? GiftClaim { get; set; }

    /// <summary>The claim, unless it's a reservation that has run out.</summary>
    public GiftClaim? GetActiveClaim(DateTime now) =>
        GiftClaim is not null && GiftClaim.IsActive(now) ? GiftClaim : null;

    public GiftClaim? GetClaimVisibleTo(Guid requestingUserId, DateTime now)
    {
        if (requestingUserId == OwnerId)
            return null;

        return GetActiveClaim(now);
    }
}
