using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Groups;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

/// <summary>
/// The checks every sign-up path runs, in this order: input (400), the invite
/// gate (403/404/410), then the duplicate-email check (409). The invite gate
/// runs before the email check so an uninvited caller can never learn whether
/// an address is registered.
/// </summary>
public class RegistrationGate(IUserRepository users, InvitationResolver invitationResolver)
{
    /// <returns>The invitation to honour. Sign-up always needs one, even on an
    /// empty system; the first account is made with the owner-access job.</returns>
    public async Task<InviteTarget> CheckAsync(
        string? email, string? displayName, string? inviteCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Invalid registration data.");
        InputLimits.Check(email, InputLimits.Email, "Email address");
        InputLimits.Check(displayName, InputLimits.DisplayName, "Name");

        if (string.IsNullOrWhiteSpace(inviteCode))
            throw new ForbiddenException("Registration is by invitation only.");
        var invitation = await invitationResolver.ResolveAsync(inviteCode, ct);
        // A drawing link for an existing account is answered by signing in.
        if (invitation.Participant?.UserId is not null)
            throw new ConflictException("This invitation belongs to an existing account. Sign in to answer it.");

        if (await users.EmailExistsAsync(email, ct))
            throw new ConflictException("An account with this email already exists.");

        return invitation;
    }
}

/// <summary>Makes a new account a member of what its invitation leads to, inside the sign-up transaction.</summary>
public class NewAccountInvitation(
    IGroupRepository groups,
    IInvitationRepository invitations,
    Drawings.AnswerDrawingUseCase drawings)
{
    public async Task RedeemAsync(InviteTarget target, Guid userId, CancellationToken ct = default)
    {
        if (target.Participant is { } participant)
        {
            await drawings.JoinNewAccountAsync(participant, userId, ct);
            return;
        }
        await groups.AddMemberAsync(target.GroupId, userId, Domain.Enums.GroupMemberRole.Member, ct);
        await invitations.MarkUsedAsync(target.Circle!.Id, ct);
    }
}
