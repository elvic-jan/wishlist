using Wishlist.Domain.Entities;

namespace Wishlist.Application.Interfaces;

/// <summary>The old wishlist site's usernames and live items.</summary>
public interface ILegacyRepository
{
    /// <summary>Whether any old data has been loaded at all.</summary>
    Task<bool> AnyAsync(CancellationToken ct = default);

    /// <param name="userName">Already normalised (trimmed, upper case).</param>
    Task<LegacyUser?> GetByUserNameAsync(string userName, CancellationToken ct = default);

    /// <summary>The old username this account imported, if any.</summary>
    Task<LegacyUser?> GetClaimedByAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<LegacyItem>> GetItemsAsync(int legacyOwnerId, CancellationToken ct = default);

    /// <summary>
    /// Marks the old username as imported by the account, only if nobody has
    /// claimed it yet. Returns false when it was claimed in the meantime.
    /// Throws <see cref="Domain.Exceptions.ConflictException"/> when the account
    /// has already claimed another username.
    /// </summary>
    Task<bool> TryClaimAsync(int legacyId, Guid userId, DateTime claimedAt, int importedItemCount, CancellationToken ct = default);

    /// <summary>Adds missing users and items and updates existing ones; claims are kept.</summary>
    Task<(int Users, int Items)> UpsertAsync(IReadOnlyList<LegacyUser> users, IReadOnlyList<LegacyItem> items, CancellationToken ct = default);
}
