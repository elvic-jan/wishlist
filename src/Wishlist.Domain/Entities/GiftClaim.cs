using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Domain.Entities;

public class GiftClaim
{
    public Guid Id { get; set; }
    public Guid WishlistItemId { get; set; }
    public Guid ClaimedByUserId { get; set; }
    public GiftClaimStatus Status { get; set; }
    public DateTime ClaimedAt { get; set; }

    /// <summary>When a reservation ends; null once bought (bought never ends).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>When the reminder for the current period was sent; null if not yet.</summary>
    public DateTime? ReminderSentAt { get; set; }

    /// <summary>False for a reservation whose end date has passed: it counts as no claim.</summary>
    public bool IsActive(DateTime now) =>
        Status == GiftClaimStatus.Bought || ExpiresAt is null || ExpiresAt > now;

    public void MarkAsBought(Guid requestingUserId)
    {
        if (ClaimedByUserId != requestingUserId)
            throw new ForbiddenException("Only the claimer can mark a gift as bought.");

        Status = GiftClaimStatus.Bought;
        ExpiresAt = null;
    }

    /// <summary>Starts a new reservation period, with a new reminder before it ends.</summary>
    public void Keep(Guid requestingUserId, DateTime now)
    {
        if (ClaimedByUserId != requestingUserId)
            throw new ForbiddenException("You can only keep your own reservation.");
        if (Status != GiftClaimStatus.Reserved)
            throw new ConflictException("Only a reservation can be kept; this gift is already bought.");
        if (!IsActive(now))
            throw new ConflictException("This reservation has run out. Reserve the gift again if it's still free.");

        ExpiresAt = ReservationSchedule.EndDateFrom(now);
        ReminderSentAt = null;
    }
}
