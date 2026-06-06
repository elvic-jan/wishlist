using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Wishlists;

public class EditWishlistItemUseCase(IWishlistRepository wishlists)
{
    public async Task<WishlistItemDto> ExecuteAsync(Guid userId, Guid itemId, EditWishlistItemRequest request, CancellationToken ct = default)
    {
        var item = await wishlists.GetByIdAsync(itemId, includeClaim: false, ct)
            ?? throw new NotFoundException("Wishlist item not found.");

        if (item.OwnerId != userId)
            throw new ForbiddenException("You can only edit your own wishlist items.");

        if (request.Title is not null)
            item.Title = string.IsNullOrWhiteSpace(request.Title) ? throw new DomainException("Title cannot be empty.") : request.Title.Trim();

        if (request.Description is not null)
            item.Description = request.Description;

        // Validate URLs up front so we don't half-persist a title change and
        // then bail on a bad URL.
        var validatedUrls = request.Urls is null ? null : WishlistItemUrlValidator.Validate(request.Urls);

        if (validatedUrls is not null)
        {
            await wishlists.ReplaceUrlsAsync(item, validatedUrls, ct);
        }
        else
        {
            await wishlists.UpdateAsync(item, ct);
        }

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
