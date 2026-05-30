using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Wishlists;

public class AddWishlistItemUseCase(IWishlistRepository wishlists)
{
    public async Task<WishlistItemDto> ExecuteAsync(Guid userId, AddWishlistItemRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new DomainException("Item title cannot be empty.");

        var item = await wishlists.CreateAsync(userId, request.Title.Trim(), request.Description, request.Url, request.Price, ct);
        return new WishlistItemDto(item.Id, item.OwnerId, item.Title, item.Description, item.Url, item.Price, item.CreatedAt, null);
    }
}
