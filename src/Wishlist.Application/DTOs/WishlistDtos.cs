using Wishlist.Domain.Enums;

namespace Wishlist.Application.DTOs;

public record AddWishlistItemRequest(string Title, string? Description, IReadOnlyList<string>? Urls);
public record EditWishlistItemRequest(string? Title, string? Description, IReadOnlyList<string>? Urls);

public record WishlistItemDto(
    Guid Id,
    Guid OwnerId,
    string Title,
    string? Description,
    IReadOnlyList<string> Urls,
    DateTime CreatedAt,
    GiftClaimDto? Claim);

/// <param name="ExpiresAt">When the reservation ends; only sent to the claimer.</param>
public record GiftClaimDto(Guid Id, Guid ClaimedByUserId, GiftClaimStatus Status, bool IsClaimedByMe, DateTime? ExpiresAt)
{
    public static GiftClaimDto For(Wishlist.Domain.Entities.GiftClaim claim, Guid requestingUserId)
    {
        var mine = claim.ClaimedByUserId == requestingUserId;
        return new GiftClaimDto(claim.Id, claim.ClaimedByUserId, claim.Status, mine, mine ? claim.ExpiresAt : null);
    }
}

public record GroupWishlistEntryDto(Guid UserId, string DisplayName);
