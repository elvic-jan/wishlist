using Wishlist.Application.Interfaces;
using Wishlist.Domain.Enums;

namespace Wishlist.Application.UseCases.Groups;

public class JoinGroupViaInvitationUseCase(
    IGroupRepository groups,
    IInvitationRepository invitations,
    InvitationResolver invitationResolver)
{
    public async Task ExecuteAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var invitation = await invitationResolver.ResolveAsync(code, ct);

        if (await groups.IsMemberAsync(invitation.GroupId, userId, ct))
            return;

        await groups.AddMemberAsync(invitation.GroupId, userId, GroupMemberRole.Member, ct);
        await invitations.MarkUsedAsync(invitation.Id, ct);
    }
}
