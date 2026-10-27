using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.GiftClaims;

/// <summary>The reserver keeps their reservation: it gets a new end date and a new reminder.</summary>
public class KeepReservationUseCase(IWishlistRepository wishlists, IGiftClaimRepository claims, TimeProvider time)
{
    public async Task<WishlistItemDto> ExecuteAsync(Guid requestingUserId, Guid ownerId, Guid itemId, CancellationToken ct = default)
    {
        var item = await wishlists.GetByIdAsync(itemId, includeClaim: true, ct)
            ?? throw new NotFoundException("Wishlist item not found.");

        if (item.OwnerId != ownerId)
            throw new NotFoundException("Wishlist item not found.");

        var claim = item.GiftClaim
            ?? throw new NotFoundException("No active claim on this item.");

        claim.Keep(requestingUserId, time.GetUtcNow().UtcDateTime);
        await claims.UpdateAsync(claim, ct);

        return WishlistItemDto.From(item, GiftClaimDto.For(claim, requestingUserId));
    }
}
