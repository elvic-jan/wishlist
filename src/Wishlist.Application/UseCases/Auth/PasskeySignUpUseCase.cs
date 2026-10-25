using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

/// <summary>
/// Sign-up with a passkey instead of a password, in two calls. The first runs
/// the registration gate and returns creation options for a freshly chosen
/// user ID; the second runs the gate again (an invite can expire, an email can
/// be taken in between), verifies the credential, and creates the account,
/// passkey and membership in one transaction.
/// </summary>
public class PasskeySignUpUseCase(
    RegistrationGate gate,
    IPasskeyService passkeys,
    IUserRepository users,
    IGroupRepository groups,
    IInvitationRepository invitations,
    IUnitOfWork unitOfWork,
    AuthSessionFactory sessions,
    AccountEmails emails)
{
    public async Task<PasskeyOptions> BeginAsync(PasskeySignUpOptionsRequest request, CancellationToken ct = default)
    {
        var (email, displayName) = Normalize(request.Email, request.DisplayName);
        await gate.CheckAsync(email, displayName, request.InviteCode, ct);
        return await passkeys.BeginRegistrationAsync(new PasskeyUser(Guid.NewGuid(), email, displayName), ct);
    }

    public async Task<(AuthResponse Response, string RefreshToken)> CompleteAsync(
        PasskeySignUpRequest request, string state, CancellationToken ct = default)
    {
        var (email, displayName) = Normalize(request.Email, request.DisplayName);
        var invitation = await gate.CheckAsync(email, displayName, request.InviteCode, ct);

        var verified = await passkeys.CompleteRegistrationAsync(request.CredentialJson, state, ct);
        if (!string.Equals(verified.User.Name, email, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(verified.User.DisplayName, displayName, StringComparison.Ordinal))
            throw new DomainException("Your details changed while the passkey was being created. Please try again.");

        User user = null!;
        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            user = await users.CreateWithoutPasswordAsync(verified.User.Id, email, displayName, innerCt);
            await passkeys.AddAsync(user.Id, verified.Credential, PasskeyNames.Clean(request.PasskeyName), innerCt);
            if (invitation is not null)
            {
                await groups.AddMemberAsync(invitation.GroupId, user.Id, GroupMemberRole.Member, innerCt);
                await invitations.MarkUsedAsync(invitation.Id, innerCt);
            }
        }, ct);

        await emails.SendEmailConfirmationAsync(user, ct);
        return await sessions.CreateAsync(user, invitation?.GroupId, ct);
    }

    private static (string Email, string DisplayName) Normalize(string? email, string? displayName) =>
        ((email ?? string.Empty).Trim(), (displayName ?? string.Empty).Trim());
}

public static class PasskeyNames
{
    public const int MaxLength = 100;

    /// <summary>Trims a user-supplied label; blank becomes <c>null</c>, overlong is cut.</summary>
    public static string? Clean(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        return trimmed.Length <= MaxLength ? trimmed : trimmed[..MaxLength];
    }
}
