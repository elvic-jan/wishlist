using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.GiftClaims;

public class ReleaseGiftClaimUseCase(IWishlistRepository wishlists, IGiftClaimRepository claims)
{
    public async Task ExecuteAsync(Guid requestingUserId, Guid ownerId, Guid itemId, CancellationToken ct = default)
    {
        var item = await wishlists.GetByIdAsync(itemId, includeClaim: true, ct)
            ?? throw new NotFoundException("Wishlist item not found.");

        if (item.OwnerId != ownerId)
            throw new NotFoundException("Wishlist item not found.");

        var claim = item.GiftClaim
            ?? throw new NotFoundException("No active claim on this item.");

        if (claim.ClaimedByUserId != requestingUserId)
            throw new ForbiddenException("You can only release your own claim.");

        await claims.DeleteAsync(claim.Id, ct);
    }
}
