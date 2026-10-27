namespace Wishlist.Domain.Entities;

/// <summary>
/// The buddy has full control over the owner's wishlist: they can add, edit and
/// delete any wish on it. It only works while the two share at least one circle.
/// </summary>
public class WishlistBuddy
{
    public Guid OwnerId { get; set; }
    public Guid BuddyId { get; set; }
    public DateTime CreatedAt { get; set; }
}
