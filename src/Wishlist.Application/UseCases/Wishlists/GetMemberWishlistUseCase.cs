using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Wishlists;

public class GetMemberWishlistUseCase(IWishlistRepository wishlists, IGroupRepository groups, TimeProvider time)
{
    public async Task<IReadOnlyList<WishlistItemDto>> ExecuteAsync(Guid requestingUserId, Guid ownerId, CancellationToken ct = default)
    {
        if (!await groups.SharesGroupAsync(requestingUserId, ownerId, ct))
            throw new ForbiddenException("You do not share a circle with this user.");

        var items = await wishlists.GetByOwnerAsync(ownerId, includeClaims: true, ct);
        var now = time.GetUtcNow().UtcDateTime;
        return items.Select(i =>
        {
            var claim = i.GetClaimVisibleTo(requestingUserId, now);
            var claimDto = claim is null ? null : GiftClaimDto.For(claim, requestingUserId);
            return new WishlistItemDto(
                i.Id,
                i.OwnerId,
                i.Title,
                i.Description,
                i.Urls.OrderBy(u => u.DisplayOrder).Select(u => u.Url).ToList(),
                i.CreatedAt,
                claimDto);
        }).ToList();
    }
}
