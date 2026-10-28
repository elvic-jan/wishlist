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
    GiftClaimDto? Claim,
    AddedByDto? AddedBy = null)
{
    public static WishlistItemDto From(Wishlist.Domain.Entities.WishlistItem item, GiftClaimDto? claim) => new(
        item.Id,
        item.OwnerId,
        item.Title,
        item.Description,
        item.Urls.OrderBy(u => u.DisplayOrder).Select(u => u.Url).ToList(),
        item.CreatedAt,
        claim,
        item.AddedById is { } id ? new AddedByDto(id, item.AddedByName ?? string.Empty) : null);
}

/// <summary>The buddy who added a wish.</summary>
public record AddedByDto(Guid UserId, string DisplayName);

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

/// <summary>Someone the user shares at least one circle with, and the circles they share.</summary>
public record ConnectedPersonDto(Guid UserId, string DisplayName, IReadOnlyList<SharedCircleDto> Circles);
public record SharedCircleDto(Guid Id, string Name);

public record BuddyDto(Guid UserId, string DisplayName);
public record AddBuddyRequest(Guid UserId);
