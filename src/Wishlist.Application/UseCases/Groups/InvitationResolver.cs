using System.Text.RegularExpressions;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Groups;

/// <summary>
/// Turns an invite code into a usable invitation for both the register and the
/// join path: unknown or malformed codes are a 404, expired ones a 410.
/// </summary>
public partial class InvitationResolver(IInvitationRepository invitations)
{
    public async Task<GroupInvitation> ResolveAsync(string code, CancellationToken ct = default)
    {
        // Codes are Guid "N" strings; anything else cannot exist, so skip the lookup.
        if (!CodeFormat().IsMatch(code))
            throw new InvitationNotFoundException();

        var invitation = await invitations.GetByCodeAsync(code, ct)
            ?? throw new InvitationNotFoundException();

        if (invitation.IsExpired)
            throw new GoneException("This invitation has expired.");

        return invitation;
    }

    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex CodeFormat();
}
