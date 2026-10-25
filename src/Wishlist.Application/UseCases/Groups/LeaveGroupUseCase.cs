using Wishlist.Application.Interfaces;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Groups;

public class LeaveGroupUseCase(IGroupRepository groups)
{
    public async Task ExecuteAsync(Guid userId, Guid groupId, CancellationToken ct = default)
    {
        var role = await groups.GetMemberRoleAsync(groupId, userId, ct)
            ?? throw new ForbiddenException("You are not a member of this circle.");

        if (role == GroupMemberRole.Owner)
        {
            var count = await groups.GetMemberCountAsync(groupId, ct);
            if (count > 1)
                throw new DomainException("Transfer ownership or remove all members before leaving as owner.");
        }

        await groups.RemoveMemberAsync(groupId, userId, ct);
    }
}
