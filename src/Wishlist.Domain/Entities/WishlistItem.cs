namespace Wishlist.Domain.Entities;

public class WishlistItem
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public IList<WishlistItemUrl> Urls { get; set; } = new List<WishlistItemUrl>();

    public GiftClaim? GiftClaim { get; set; }

    public GiftClaim? GetClaimVisibleTo(Guid requestingUserId)
    {
        if (requestingUserId == OwnerId)
            return null;

        return GiftClaim;
    }
}
