using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Buddies;

/// <summary>The owner picks the buddies who get full control over their wishlist.</summary>
public class ManageBuddiesUseCase(
    IWishlistBuddyRepository buddies,
    IGroupRepository groups,
    IUserRepository users,
    BuddyEmails emails,
    TimeProvider time)
{
    public async Task<IReadOnlyList<BuddyDto>> GetBuddiesAsync(Guid ownerId, CancellationToken ct = default) =>
        ToDtos(await buddies.GetBuddiesAsync(ownerId, ct));

    public async Task<IReadOnlyList<BuddyDto>> GetCandidatesAsync(Guid ownerId, CancellationToken ct = default) =>
        ToDtos(await buddies.GetCandidatesAsync(ownerId, ct));

    /// <summary>The wishlists this user is a buddy for.</summary>
    public async Task<IReadOnlyList<BuddyDto>> GetBuddyOfAsync(Guid userId, CancellationToken ct = default) =>
        ToDtos(await buddies.GetBuddyOfAsync(userId, ct));

    public async Task<BuddyDto> AddAsync(Guid ownerId, Guid buddyId, CancellationToken ct = default)
    {
        if (buddyId == ownerId)
            throw new DomainException("You can't be your own buddy.");

        if (!await groups.SharesGroupAsync(ownerId, buddyId, ct))
            throw new ForbiddenException("You can only pick someone you share a circle with.");

        var buddy = await users.GetByIdAsync(buddyId, ct) ?? throw new NotFoundException("User not found.");
        var owner = await users.GetByIdAsync(ownerId, ct) ?? throw new NotFoundException("User not found.");

        if (await buddies.AddAsync(ownerId, buddyId, time.GetUtcNow().UtcDateTime, ct))
            await emails.SendNewBuddyAsync(buddy, owner, ct);

        return new BuddyDto(buddy.Id, buddy.DisplayName);
    }

    /// <summary>Wishes the buddy already added stay on the list.</summary>
    public Task RemoveAsync(Guid ownerId, Guid buddyId, CancellationToken ct = default) =>
        buddies.RemoveAsync(ownerId, buddyId, ct);

    private static IReadOnlyList<BuddyDto> ToDtos(IReadOnlyList<(Guid UserId, string DisplayName)> rows) =>
        rows.Select(r => new BuddyDto(r.UserId, r.DisplayName)).ToList();
}
