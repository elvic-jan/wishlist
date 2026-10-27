using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Wishlists;

/// <summary>Who may change someone else's wishlist: a buddy who still shares a circle with the owner.</summary>
public class WishlistAccess(IWishlistBuddyRepository buddies)
{
    /// <summary>For the buddy routes (/wishlists/{ownerId}/items): the caller must be an active buddy of the owner.</summary>
    public async Task EnsureBuddyAsync(Guid callerId, Guid ownerId, CancellationToken ct = default)
    {
        if (callerId == ownerId)
            throw new DomainException("Use your own wishlist to change your own wishes.");

        if (!await buddies.IsActiveBuddyAsync(ownerId, callerId, ct))
            throw new ForbiddenException("You are not a buddy for this wishlist.");
    }
}
