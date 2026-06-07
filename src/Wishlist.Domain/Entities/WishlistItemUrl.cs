namespace Wishlist.Domain.Entities;

public class WishlistItemUrl
{
    public Guid Id { get; set; }
    public Guid WishlistItemId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
