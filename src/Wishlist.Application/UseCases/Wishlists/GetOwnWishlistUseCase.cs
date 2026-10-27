using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;

namespace Wishlist.Application.UseCases.Wishlists;

public class GetOwnWishlistUseCase(IWishlistRepository wishlists)
{
    public async Task<IReadOnlyList<WishlistItemDto>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await wishlists.GetByOwnerAsync(userId, includeClaims: false, ct);
        return items
            .Select(i => WishlistItemDto.From(i, null))
            .ToList();
    }
}
