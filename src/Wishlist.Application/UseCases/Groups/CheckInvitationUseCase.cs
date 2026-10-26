using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;

namespace Wishlist.Application.UseCases.Groups;

/// <summary>
/// Lets the SPA tell a visitor straight away whether their invite link works,
/// and who invited them to which circle, before they fill in the sign-up form.
/// Also feeds the invite link preview. Same 404/410 outcomes as register/join.
/// </summary>
public class CheckInvitationUseCase(InvitationResolver invitationResolver, IGroupRepository groups, IUserRepository users)
{
    public const string UnknownInviter = "Someone";

    public async Task<InvitationStatusDto> ExecuteAsync(string code, CancellationToken ct = default)
    {
        var invitation = await invitationResolver.ResolveAsync(code, ct);
        var group = await groups.GetByIdAsync(invitation.GroupId, ct);
        var inviter = await users.GetByIdAsync(invitation.CreatedByUserId, ct);

        return new InvitationStatusDto(
            invitation.ExpiresAt,
            inviter?.DisplayName ?? UnknownInviter,
            group?.Name ?? "a circle");
    }
}
