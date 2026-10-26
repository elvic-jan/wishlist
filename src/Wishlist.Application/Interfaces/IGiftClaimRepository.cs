using Wishlist.Domain.Entities;

namespace Wishlist.Application.Interfaces;

public interface IGiftClaimRepository
{
    Task<GiftClaim?> GetByItemIdAsync(Guid wishlistItemId, CancellationToken ct = default);
    Task<GiftClaim> CreateAsync(Guid wishlistItemId, Guid claimedByUserId, DateTime claimedAt, DateTime expiresAt, CancellationToken ct = default);

    /// <summary>Saves the claim's status, end date and reminder time.</summary>
    Task UpdateAsync(GiftClaim claim, CancellationToken ct = default);
    Task DeleteAsync(Guid claimId, CancellationToken ct = default);

    /// <summary>Reservations still running, ending at or before <paramref name="endsBy"/>, whose reminder hasn't been sent.</summary>
    Task<IReadOnlyList<ReservationNotice>> GetReminderDueAsync(DateTime now, DateTime endsBy, CancellationToken ct = default);

    /// <summary>Reservations whose end date is at or before <paramref name="now"/>.</summary>
    Task<IReadOnlyList<ReservationNotice>> GetExpiredAsync(DateTime now, CancellationToken ct = default);

    Task MarkReminderSentAsync(Guid claimId, DateTime sentAt, CancellationToken ct = default);
}

/// <summary>What a reservation email needs to know about one reservation.</summary>
public record ReservationNotice(Guid ClaimId, Guid ClaimerId, Guid OwnerId, string ItemTitle, DateTime ExpiresAt);
