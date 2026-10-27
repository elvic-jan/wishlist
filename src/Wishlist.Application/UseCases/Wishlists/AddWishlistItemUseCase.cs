using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Wishlists;

public class AddWishlistItemUseCase(IWishlistRepository wishlists, WishlistAccess access)
{
    /// <summary>The owner adds a wish to their own list.</summary>
    public Task<WishlistItemDto> ExecuteAsync(Guid userId, AddWishlistItemRequest request, CancellationToken ct = default) =>
        AddAsync(userId, addedById: null, request, ct);

    /// <summary>A buddy adds a wish to the owner's list; the wish records who added it.</summary>
    public async Task<WishlistItemDto> ExecuteAsBuddyAsync(Guid buddyId, Guid ownerId, AddWishlistItemRequest request, CancellationToken ct = default)
    {
        await access.EnsureBuddyAsync(buddyId, ownerId, ct);
        return await AddAsync(ownerId, buddyId, request, ct);
    }

    private async Task<WishlistItemDto> AddAsync(Guid ownerId, Guid? addedById, AddWishlistItemRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new DomainException("Item title cannot be empty.");

        var urls = WishlistItemUrlValidator.Validate(request.Urls);

        var item = await wishlists.CreateAsync(ownerId, request.Title.Trim(), request.Description, urls, ct, addedById);

        return WishlistItemDto.From(item, null);
    }
}
