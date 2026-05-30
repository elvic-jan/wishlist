using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;

namespace Wishlist.Application.UseCases.Groups;

public class GetUserGroupsUseCase(IGroupRepository groups)
{
    public async Task<IReadOnlyList<GroupDto>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var userGroups = await groups.GetUserGroupsAsync(userId, ct);
        return userGroups.Select(g => new GroupDto(g.Id, g.Name, g.CreatedAt)).ToList();
    }
}
