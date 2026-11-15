using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;

namespace Wishlist.Application.UseCases.Drawings;

/// <summary>
/// A drawing as the organizer or a participant sees it. A participant's
/// wishlist can be opened only when the viewer shares a connecting circle with
/// them (both joined the drawing, or a permanent circle). An address the
/// organizer typed is shown by its typed name until that person joins, so
/// nothing reveals whether it belongs to an account. No one's recipient is
/// included except the viewer's own.
/// </summary>
public class GetDrawingUseCase(
    IDrawingRepository drawings,
    IGroupRepository groups,
    IUserRepository users,
    IWishlistBuddyRepository buddies,
    DrawingAccess access)
{
    public async Task<DrawingDto> ExecuteAsync(Guid userId, Guid drawingId, CancellationToken ct = default)
    {
        var state = await access.LoadVisibleAsync(userId, drawingId, ct);
        var isOrganizer = DrawingAccess.IsOrganizer(state, userId);
        var me = state.ParticipantOf(userId);
        var meJoined = me?.Status == DrawingParticipantStatus.Joined;

        var names = await DrawingNames.LoadAsync(groups, drawingId, ct);
        var reachable = await DrawingPeople.ReachableAsync(groups, userId, ct);
        var buddyOf = meJoined
            ? (await buddies.GetBuddyOfAsync(userId, ct)).Select(b => b.UserId).ToHashSet()
            : [];

        var participants = state.Participants
            .Where(p => isOrganizer || p.Status != DrawingParticipantStatus.Declined || p.Id == me?.Id)
            .Select(p =>
            {
                var hidesAccount = p.IsInvitedByEmail && p.Status != DrawingParticipantStatus.Joined;
                var canJoinForThem = meJoined && p.Status == DrawingParticipantStatus.Invited
                    && p.UserId is { } friendId && friendId != userId && buddyOf.Contains(friendId)
                    && !(hidesAccount && isOrganizer);
                return new DrawingParticipantDto(
                    p.Id,
                    DrawingNames.Of(p, names),
                    DrawingStatuses.Of(p.Status),
                    DrawingPeople.VisibleUserId(p, reachable),
                    isOrganizer && p.Status != DrawingParticipantStatus.Joined ? p.Email : null,
                    p.Id == me?.Id,
                    canJoinForThem);
            })
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        DrawingRecipientDto? recipient = null;
        if (state.Drawing.IsDrawn && me is not null && me.Status != DrawingParticipantStatus.Declined)
        {
            var assignment = await drawings.GetAssignmentByGiverAsync(me.Id, ct);
            var receiver = assignment is null ? null : state.Participants.FirstOrDefault(p => p.Id == assignment.ReceiverParticipantId);
            if (receiver is not null)
                recipient = new DrawingRecipientDto(drawingId, state.Group.Name, DrawingNames.Of(receiver, names),
                    DrawingPeople.VisibleUserId(receiver, reachable), receiver.Status == DrawingParticipantStatus.Joined);
        }

        var organizer = state.Drawing.OrganizerId is { } organizerId ? await users.GetByIdAsync(organizerId, ct) : null;
        return new DrawingDto(
            drawingId,
            state.Group.Name,
            state.Drawing.EndDate,
            state.Drawing.Budget,
            organizer?.DisplayName ?? "",
            isOrganizer,
            state.Drawing.IsDrawn,
            state.Drawing.IsCancelled,
            state.Drawing.IsEnded(access.Today),
            me?.Id,
            me is null ? null : DrawingStatuses.Of(me.Status),
            participants,
            isOrganizer
                ? state.Exclusions.Select(x => new DrawingExclusionPair(x.FirstParticipantId, x.SecondParticipantId)).ToList()
                : [],
            recipient);
    }
}

/// <summary>The user's unanswered drawing invitations, for the Join / Decline cards.</summary>
public class GetMyDrawingInvitationsUseCase(IDrawingRepository drawings, IUserRepository users, DrawingAccess access)
{
    public async Task<IReadOnlyList<DrawingInvitationDto>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var mine = await drawings.GetForUserAsync(userId, access.Now, ct);
        var result = new List<DrawingInvitationDto>();
        foreach (var d in mine.Where(d => d.Me?.Status == DrawingParticipantStatus.Invited && d.Drawing.IsActive(access.Today)))
        {
            var organizer = d.Drawing.OrganizerId is { } id ? await users.GetByIdAsync(id, ct) : null;
            result.Add(new DrawingInvitationDto(d.Group.Id, d.Me!.Id, d.Group.Name, organizer?.DisplayName ?? "",
                d.Drawing.EndDate, d.Drawing.Budget, d.Drawing.IsDrawn));
        }
        return result.OrderBy(i => i.EndDate).ToList();
    }
}

/// <summary>The people the user drew, for the top of the Wishlists page.</summary>
public class GetMyRecipientsUseCase(IDrawingRepository drawings, IGroupRepository groups, DrawingAccess access)
{
    public async Task<IReadOnlyList<DrawingRecipientDto>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await drawings.GetMyRecipientsAsync(userId, access.Today, ct);
        if (rows.Count == 0) return [];
        var reachable = await DrawingPeople.ReachableAsync(groups, userId, ct);
        return rows
            .Select(r => new DrawingRecipientDto(
                r.DrawingId,
                r.DrawingTitle,
                r.ReceiverName,
                r.ReceiverUserId is { } id && !(r.ReceiverInvitedByEmail && !r.ReceiverJoined) && reachable.Contains(id) ? id : null,
                r.ReceiverJoined))
            .OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}

internal static class DrawingPeople
{
    /// <summary>Everyone whose wishlist the user may open (shares a connecting circle with).</summary>
    public static async Task<HashSet<Guid>> ReachableAsync(IGroupRepository groups, Guid userId, CancellationToken ct) =>
        (await groups.GetCircleMatesAsync(userId, ct)).Select(m => m.UserId).ToHashSet();

    /// <summary>The participant's account, when the viewer may open their wishlist and it doesn't reveal a typed address's account.</summary>
    public static Guid? VisibleUserId(DrawingParticipant p, IReadOnlySet<Guid> reachable) =>
        p.UserId is { } id && !(p.IsInvitedByEmail && p.Status != DrawingParticipantStatus.Joined) && reachable.Contains(id)
            ? id
            : null;
}
