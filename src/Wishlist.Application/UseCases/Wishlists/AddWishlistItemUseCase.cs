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

        var urls = WishlistItemUrlValidator.Validate(request.Urls);

        var item = await wishlists.CreateAsync(userId, request.Title.Trim(), request.Description, urls, ct);

        return new WishlistItemDto(
            item.Id,
            item.OwnerId,
            item.Title,
            item.Description,
            item.Urls.OrderBy(u => u.DisplayOrder).Select(u => u.Url).ToList(),
            item.CreatedAt,
            Claim: null);
    }
}
