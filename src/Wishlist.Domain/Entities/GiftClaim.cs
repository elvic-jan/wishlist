using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Domain.Entities;

public class GiftClaim
{
    public Guid Id { get; set; }
    public Guid WishlistItemId { get; set; }
    public Guid ClaimedByUserId { get; set; }
    public GiftClaimStatus Status { get; set; }
    public DateTime ClaimedAt { get; set; }

    public void MarkAsBought(Guid requestingUserId)
    {
        if (ClaimedByUserId != requestingUserId)
            throw new ForbiddenException("Only the claimer can mark a gift as bought.");

        Status = GiftClaimStatus.Bought;
    }
}
