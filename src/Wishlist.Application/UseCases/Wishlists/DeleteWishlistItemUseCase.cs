using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Wishlists;

public class DeleteWishlistItemUseCase(IWishlistRepository wishlists, WishlistAccess access)
{
    /// <summary>The owner deletes a wish from their own list (and any claim on it).</summary>
    public async Task ExecuteAsync(Guid userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await wishlists.GetByIdAsync(itemId, false, ct)
            ?? throw new NotFoundException("Wishlist item not found.");

        if (item.OwnerId != userId)
            throw new ForbiddenException("You can only delete your own wishlist items.");

        await wishlists.DeleteAsync(itemId, ct);
    }

    /// <summary>A buddy deletes any wish on the owner's list (and any claim on it).</summary>
    public async Task ExecuteAsBuddyAsync(Guid buddyId, Guid ownerId, Guid itemId, CancellationToken ct = default)
    {
        await access.EnsureBuddyAsync(buddyId, ownerId, ct);

        var item = await wishlists.GetByIdAsync(itemId, false, ct);
        if (item is null || item.OwnerId != ownerId)
            throw new NotFoundException("Wishlist item not found.");

        await wishlists.DeleteAsync(itemId, ct);
    }
}
