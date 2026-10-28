using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Groups;

public class CreateGroupUseCase(IGroupRepository groups)
{
    public async Task<GroupDto> ExecuteAsync(Guid userId, CreateGroupRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new DomainException("Circle name cannot be empty.");
        InputLimits.Check(request.Name.Trim(), InputLimits.CircleName, "Circle name");

        var group = await groups.CreateAsync(request.Name.Trim(), userId, ct);
        await groups.AddMemberAsync(group.Id, userId, GroupMemberRole.Owner, ct);

        return new GroupDto(group.Id, group.Name, group.CreatedAt);
    }
}
