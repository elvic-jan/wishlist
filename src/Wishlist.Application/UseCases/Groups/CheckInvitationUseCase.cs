using Wishlist.Application.DTOs;

namespace Wishlist.Application.UseCases.Groups;

/// <summary>
/// Lets the SPA tell a visitor straight away whether their invite link works,
/// before they fill in the sign-up form. Same 404/410 outcomes as register/join.
/// </summary>
public class CheckInvitationUseCase(InvitationResolver invitationResolver)
{
    public async Task<InvitationStatusDto> ExecuteAsync(string code, CancellationToken ct = default)
    {
        var invitation = await invitationResolver.ResolveAsync(code, ct);
        return new InvitationStatusDto(invitation.ExpiresAt);
    }
}
