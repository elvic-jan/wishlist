namespace Wishlist.Application.Interfaces;

/// <summary>
/// Buddy relationships. Every "active" check and list also requires the owner
/// and buddy to share at least one circle; rows for people who no longer do are
/// kept but ignored.
/// </summary>
public interface IWishlistBuddyRepository
{
    /// <summary>Returns true when the buddy was newly added, false when they already were one.</summary>
    Task<bool> AddAsync(Guid ownerId, Guid buddyId, DateTime now, CancellationToken ct = default);
    Task RemoveAsync(Guid ownerId, Guid buddyId, CancellationToken ct = default);
    Task<bool> IsActiveBuddyAsync(Guid ownerId, Guid buddyId, CancellationToken ct = default);

    /// <summary>The owner's buddies who still share a circle with them.</summary>
    Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetBuddiesAsync(Guid ownerId, CancellationToken ct = default);

    /// <summary>The owners whose buddy this user is, while they share a circle.</summary>
    Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetBuddyOfAsync(Guid buddyId, CancellationToken ct = default);

    /// <summary>People sharing a circle with the owner, minus the owner and their current buddies.</summary>
    Task<IReadOnlyList<(Guid UserId, string DisplayName)>> GetCandidatesAsync(Guid ownerId, CancellationToken ct = default);
}
