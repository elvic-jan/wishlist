using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Wishlists;

public class DeleteWishlistItemUseCase(IWishlistRepository wishlists)
{
    public async Task ExecuteAsync(Guid userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await wishlists.GetByIdAsync(itemId, false, ct)
            ?? throw new NotFoundException("Wishlist item not found.");

        if (item.OwnerId != userId)
            throw new ForbiddenException("You can only delete your own wishlist items.");

        await wishlists.DeleteAsync(itemId, ct);
    }
}
