using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Groups;

public class ListGroupMembersUseCase(IGroupRepository groups)
{
    public async Task<IReadOnlyList<GroupMemberDto>> ExecuteAsync(Guid userId, Guid groupId, CancellationToken ct = default)
    {
        if (!await groups.IsMemberAsync(groupId, userId, ct))
            throw new ForbiddenException("You are not a member of this group.");

        var members = await groups.GetMembersAsync(groupId, ct);
        return members.Select(m => new GroupMemberDto(m.UserId, m.DisplayName, m.Role)).ToList();
    }
}
