using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Wishlists;

public class ListGroupWishlistsUseCase(IGroupRepository groups, IWishlistRepository wishlists)
{
    public async Task<IReadOnlyList<GroupWishlistEntryDto>> ExecuteAsync(Guid userId, Guid groupId, CancellationToken ct = default)
    {
        if (!await groups.IsMemberAsync(groupId, userId, ct))
            throw new ForbiddenException("You are not a member of this circle.");

        var members = await wishlists.GetGroupMemberSummariesAsync(groupId, userId, ct);
        return members.Select(m => new GroupWishlistEntryDto(m.UserId, m.DisplayName)).ToList();
    }
}
