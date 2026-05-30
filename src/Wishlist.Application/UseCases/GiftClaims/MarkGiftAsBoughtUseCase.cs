using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.GiftClaims;

public class MarkGiftAsBoughtUseCase(IWishlistRepository wishlists, IGiftClaimRepository claims)
{
    public async Task<WishlistItemDto> ExecuteAsync(Guid requestingUserId, Guid ownerId, Guid itemId, CancellationToken ct = default)
    {
        var item = await wishlists.GetByIdAsync(itemId, includeClaim: true, ct)
            ?? throw new NotFoundException("Wishlist item not found.");

        if (item.OwnerId != ownerId)
            throw new NotFoundException("Wishlist item not found.");

        var claim = item.GiftClaim
            ?? throw new NotFoundException("No active claim on this item.");

        claim.MarkAsBought(requestingUserId);
        await claims.UpdateStatusAsync(claim.Id, GiftClaimStatus.Bought, ct);

        var claimDto = new GiftClaimDto(claim.Id, claim.ClaimedByUserId, claim.Status, true);
        return new WishlistItemDto(item.Id, item.OwnerId, item.Title, item.Description, item.Url, item.Price, item.CreatedAt, claimDto);
    }
}
