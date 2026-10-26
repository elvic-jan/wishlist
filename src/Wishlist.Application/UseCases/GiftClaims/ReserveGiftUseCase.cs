using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.GiftClaims;

public class ReserveGiftUseCase(IWishlistRepository wishlists, IGiftClaimRepository claims, IGroupRepository groups, TimeProvider time)
{
    public async Task<WishlistItemDto> ExecuteAsync(Guid requestingUserId, Guid ownerId, Guid itemId, CancellationToken ct = default)
    {
        var item = await wishlists.GetByIdAsync(itemId, includeClaim: true, ct)
            ?? throw new NotFoundException("Wishlist item not found.");

        if (item.OwnerId != ownerId)
            throw new NotFoundException("Wishlist item not found.");

        if (item.OwnerId == requestingUserId)
            throw new ForbiddenException("You cannot claim an item from your own wishlist.");

        if (!await groups.SharesGroupAsync(requestingUserId, ownerId, ct))
            throw new ForbiddenException("You do not share a circle with this user.");

        var now = time.GetUtcNow().UtcDateTime;
        if (item.GetActiveClaim(now) is not null)
            throw new ConflictException("This item has already been claimed.");

        // A reservation that ran out counts as no claim; make room for the new one.
        if (item.GiftClaim is not null)
            await claims.DeleteAsync(item.GiftClaim.Id, ct);

        var claim = await claims.CreateAsync(itemId, requestingUserId, now, ReservationSchedule.EndDateFrom(now), ct);
        var claimDto = GiftClaimDto.For(claim, requestingUserId);
        return new WishlistItemDto(
            item.Id,
            item.OwnerId,
            item.Title,
            item.Description,
            item.Urls.OrderBy(u => u.DisplayOrder).Select(u => u.Url).ToList(),
            item.CreatedAt,
            claimDto);
    }
}
