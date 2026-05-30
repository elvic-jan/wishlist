using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Wishlists;

public class EditWishlistItemUseCase(IWishlistRepository wishlists)
{
    public async Task<WishlistItemDto> ExecuteAsync(Guid userId, Guid itemId, EditWishlistItemRequest request, CancellationToken ct = default)
    {
        var item = await wishlists.GetByIdAsync(itemId, false, ct)
            ?? throw new NotFoundException("Wishlist item not found.");

        if (item.OwnerId != userId)
            throw new ForbiddenException("You can only edit your own wishlist items.");

        if (request.Title is not null)
            item.Title = string.IsNullOrWhiteSpace(request.Title) ? throw new DomainException("Title cannot be empty.") : request.Title.Trim();

        if (request.Description is not null) item.Description = request.Description;
        if (request.Url is not null) item.Url = request.Url;
        if (request.Price is not null) item.Price = request.Price;

        await wishlists.UpdateAsync(item, ct);
        return new WishlistItemDto(item.Id, item.OwnerId, item.Title, item.Description, item.Url, item.Price, item.CreatedAt, null);
    }
}
