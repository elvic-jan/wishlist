using System.Text.RegularExpressions;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Groups;

/// <summary>
/// What an invite code leads to: a circle's shared invitation link, or one
/// participant's personal link to a Secret Santa drawing.
/// </summary>
public record InviteTarget(Guid GroupId, GroupInvitation? Circle, DrawingParticipant? Participant, Drawing? Drawing);

/// <summary>
/// Turns an invite code into a usable invitation for the register, join and
/// check paths: unknown or malformed codes are a 404, expired ones a 410.
/// A drawing code works until its drawing is cancelled or expires, and only
/// while its participant hasn't declined (or been removed).
/// </summary>
public partial class InvitationResolver(IInvitationRepository invitations, IDrawingRepository drawings, TimeProvider time)
{
    public async Task<InviteTarget> ResolveAsync(string code, CancellationToken ct = default)
    {
        // Codes are Guid "N" strings; anything else cannot exist, so skip the lookup.
        if (!CodeFormat().IsMatch(code))
            throw new InvitationNotFoundException();

        var invitation = await invitations.GetByCodeAsync(code, ct);
        if (invitation is not null)
        {
            if (invitation.IsExpired)
                throw new GoneException("This invitation has expired.");
            return new InviteTarget(invitation.GroupId, invitation, null, null);
        }

        var participant = await drawings.GetParticipantByCodeAsync(code, ct)
            ?? throw new InvitationNotFoundException();
        var state = await drawings.GetAsync(participant.GroupId, ct);
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        if (state is null || !state.Drawing.IsActive(today) || participant.Status == DrawingParticipantStatus.Declined)
            throw new InvitationNotFoundException();

        return new InviteTarget(participant.GroupId, null, participant, state.Drawing);
    }

    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex CodeFormat();
}
