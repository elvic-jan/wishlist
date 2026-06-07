using Wishlist.Domain.Entities;

namespace Wishlist.Application.Interfaces;

public interface IWishlistRepository
{
    Task<IReadOnlyList<WishlistItem>> GetByOwnerAsync(Guid ownerId, bool includeClaims, CancellationToken ct = default);
    Task<WishlistItem?> GetByIdAsync(Guid itemId, bool includeClaim, CancellationToken ct = default);
    Task<WishlistItem> CreateAsync(Guid ownerId, string title, string? description, IReadOnlyList<string> urls, CancellationToken ct = default);
    Task UpdateAsync(WishlistItem item, CancellationToken ct = default);
    Task ReplaceUrlsAsync(WishlistItem item, IReadOnlyList<string> urls, CancellationToken ct = default);
    Task DeleteAsync(Guid itemId, CancellationToken ct = default);
    Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetGroupMemberSummariesAsync(Guid groupId, Guid excludeUserId, CancellationToken ct = default);
}
