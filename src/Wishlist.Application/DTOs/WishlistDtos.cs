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

public record GiftClaimDto(Guid Id, Guid ClaimedByUserId, GiftClaimStatus Status, bool IsClaimedByMe);

public record GroupWishlistEntryDto(Guid UserId, string DisplayName);
