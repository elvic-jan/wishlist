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
    /// <returns>The invitation to honour, or <c>null</c> for the first-account bootstrap.</returns>
    public async Task<GroupInvitation?> CheckAsync(
        string? email, string? displayName, string? inviteCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Invalid registration data.");
        InputLimits.Check(email, InputLimits.Email, "Email address");
        InputLimits.Check(displayName, InputLimits.DisplayName, "Name");

        var invitation = string.IsNullOrWhiteSpace(inviteCode)
            ? await RequireBootstrapAsync(ct)
            : await invitationResolver.ResolveAsync(inviteCode, ct);

        if (await users.EmailExistsAsync(email, ct))
            throw new ConflictException("An account with this email already exists.");

        return invitation;
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
