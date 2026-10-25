using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

public class RegisterUserUseCase(
    IUserRepository users,
    IGroupRepository groups,
    IInvitationRepository invitations,
    RegistrationGate gate,
    IUnitOfWork unitOfWork,
    AuthSessionFactory sessions,
    AccountEmails emails)
{
    public async Task<(AuthResponse Response, string RefreshToken)> ExecuteAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (request.Password is null || request.Password.Length < 8)
            throw new DomainException("Invalid registration data.");

        var invitation = await gate.CheckAsync(request.Email, request.DisplayName, request.InviteCode, ct);

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

        await emails.SendEmailConfirmationAsync(user, ct);
        return await sessions.CreateAsync(user, invitation?.GroupId, ct);
    }
}
