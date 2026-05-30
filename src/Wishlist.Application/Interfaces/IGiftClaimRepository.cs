using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;

namespace Wishlist.Application.Interfaces;

public interface IGiftClaimRepository
{
    Task<GiftClaim?> GetByItemIdAsync(Guid wishlistItemId, CancellationToken ct = default);
    Task<GiftClaim> CreateAsync(Guid wishlistItemId, Guid claimedByUserId, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid claimId, GiftClaimStatus status, CancellationToken ct = default);
    Task DeleteAsync(Guid claimId, CancellationToken ct = default);
}
