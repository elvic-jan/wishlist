using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.GiftClaims;

public class MarkGiftAsBoughtUseCase(IWishlistRepository wishlists, IGiftClaimRepository claims, TimeProvider time)
{
    public async Task<WishlistItemDto> ExecuteAsync(Guid requestingUserId, Guid ownerId, Guid itemId, CancellationToken ct = default)
    {
        var item = await wishlists.GetByIdAsync(itemId, includeClaim: true, ct)
            ?? throw new NotFoundException("Wishlist item not found.");

        if (item.OwnerId != ownerId)
            throw new NotFoundException("Wishlist item not found.");

        var claim = item.GetActiveClaim(time.GetUtcNow().UtcDateTime)
            ?? throw new NotFoundException("No active claim on this item.");

        claim.MarkAsBought(requestingUserId);
        await claims.UpdateAsync(claim, ct);

        var claimDto = GiftClaimDto.For(claim, requestingUserId);
        return WishlistItemDto.From(item, claimDto);
    }
}
