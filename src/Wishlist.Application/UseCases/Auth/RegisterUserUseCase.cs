using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Groups;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

public class RegisterUserUseCase(
    IUserRepository users,
    ITokenService tokens,
    IGroupRepository groups,
    IInvitationRepository invitations,
    InvitationResolver invitationResolver,
    IUnitOfWork unitOfWork)
{
    public async Task<(AuthResponse Response, string RefreshToken)> ExecuteAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.DisplayName) ||
            request.Password.Length < 8)
            throw new DomainException("Invalid registration data.");

        // The invite gate runs before the email check so an uninvited caller can
        // never learn whether an address is registered.
        var invitation = string.IsNullOrWhiteSpace(request.InviteCode)
            ? await RequireBootstrapAsync(ct)
            : await invitationResolver.ResolveAsync(request.InviteCode, ct);

        if (await users.EmailExistsAsync(request.Email, ct))
            throw new ConflictException("An account with this email already exists.");

        // Account and membership commit together: no account may exist without
        // the group it was invited into.
        User user = null!;
        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            user = await users.CreateAsync(request.Email, request.DisplayName, request.Password, innerCt);
            if (invitation is not null)
            {
                await groups.AddMemberAsync(invitation.GroupId, user.Id, GroupMemberRole.Member, innerCt);
                await invitations.MarkUsedAsync(invitation.Id, innerCt);
            }
        }, ct);

        var accessToken = tokens.GenerateAccessToken(user);
        var refreshToken = await tokens.CreateRefreshTokenAsync(user.Id, ct);

        return (new AuthResponse(accessToken, new UserDto(user.Id, user.Email, user.DisplayName), invitation?.GroupId),
            refreshToken.Token);
    }

    // First-account bootstrap: a codeless sign-up is allowed only while no user
    // exists. Temporary — slated for removal so registration always needs an
    // invite (see add-invite-only-signups design, Open Questions).
    private async Task<GroupInvitation?> RequireBootstrapAsync(CancellationToken ct)
    {
        if (await users.AnyAsync(ct))
            throw new ForbiddenException("Registration is by invitation only.");
        return null;
    }
}
