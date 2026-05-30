using Wishlist.Application.Interfaces;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Groups;

public class JoinGroupViaInvitationUseCase(IGroupRepository groups, IInvitationRepository invitations)
{
    public async Task ExecuteAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var invitation = await invitations.GetByCodeAsync(code, ct)
            ?? throw new NotFoundException("Invitation not found.");

        if (invitation.IsExpired)
            throw new GoneException("This invitation has expired.");

        if (await groups.IsMemberAsync(invitation.GroupId, userId, ct))
            return;

        await groups.AddMemberAsync(invitation.GroupId, userId, GroupMemberRole.Member, ct);
        await invitations.MarkUsedAsync(invitation.Id, ct);
    }
}
