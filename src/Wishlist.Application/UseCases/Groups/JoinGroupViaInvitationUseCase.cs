using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Drawings;
using Wishlist.Domain.Enums;

namespace Wishlist.Application.UseCases.Groups;

/// <summary>A signed-in user follows an invite link: a circle's shared link, or their personal drawing link.</summary>
public class JoinGroupViaInvitationUseCase(
    IGroupRepository groups,
    IInvitationRepository invitations,
    InvitationResolver invitationResolver,
    AnswerDrawingUseCase drawings)
{
    /// <summary>Returns the circle's (or drawing's) id.</summary>
    public async Task<Guid> ExecuteAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var target = await invitationResolver.ResolveAsync(code, ct);
        if (target.Participant is not null)
            return await drawings.JoinWithCodeAsync(userId, target, ct);

        var invitation = target.Circle!;
        if (await groups.IsMemberAsync(invitation.GroupId, userId, ct))
            return invitation.GroupId;

        await groups.AddMemberAsync(invitation.GroupId, userId, GroupMemberRole.Member, ct);
        await invitations.MarkUsedAsync(invitation.Id, ct);
        return invitation.GroupId;
    }
}
