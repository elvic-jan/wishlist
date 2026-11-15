using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Groups;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Drawings;

/// <summary>
/// Joining and declining a drawing: from the in-app card, through the personal
/// link, for a friend whose wishlist you are a buddy for, or by leaving later.
/// A participant who joins becomes a member of the drawing's circle; one who
/// declines or leaves stops being one.
/// </summary>
public class AnswerDrawingUseCase(
    IDrawingRepository drawings,
    IGroupRepository groups,
    IUserRepository users,
    IWishlistBuddyRepository buddies,
    IUnitOfWork unitOfWork,
    InvitationResolver invitationResolver,
    DrawingEmails emails,
    DrawingAccess access)
{
    /// <summary>
    /// Joins <paramref name="participantId"/>: the caller themself, or someone
    /// whose wishlist the caller is a buddy for, once the caller has joined.
    /// </summary>
    public async Task JoinAsync(Guid userId, Guid drawingId, Guid participantId, CancellationToken ct = default)
    {
        var state = await access.LoadActiveAsync(userId, drawingId, ct);
        var participant = state.Participants.FirstOrDefault(p => p.Id == participantId)
            ?? throw new NotFoundException("Participant not found.");

        if (participant.UserId != userId)
        {
            var me = state.ParticipantOf(userId);
            if (me?.Status != DrawingParticipantStatus.Joined || participant.UserId is not { } friendId
                || participant.Status != DrawingParticipantStatus.Invited
                || !await buddies.IsActiveBuddyAsync(friendId, userId, ct))
                throw new ForbiddenException("You can only join for someone whose wishlist you are a buddy for.");
        }

        if (participant.Status == DrawingParticipantStatus.Joined) return;
        await JoinAsUserAsync(participant, participant.UserId!.Value, ct);
    }

    /// <summary>Joins through a personal link while signed in. Returns the drawing's id.</summary>
    public async Task<Guid> JoinWithCodeAsync(Guid userId, InviteTarget target, CancellationToken ct = default)
    {
        var participant = target.Participant!;
        if (participant.UserId is { } owner && owner != userId)
            throw new InvitationNotFoundException();

        var state = await drawings.GetAsync(participant.GroupId, ct) ?? throw new InvitationNotFoundException();
        var other = state.ParticipantOf(userId);
        if (other is not null && other.Id != participant.Id)
            throw new ConflictException("You're already taking part in this drawing.");

        if (participant.Status != DrawingParticipantStatus.Joined)
            await JoinAsUserAsync(participant, userId, ct);
        return participant.GroupId;
    }

    /// <summary>Joins a new account in the same transaction that creates it (registration with a drawing link).</summary>
    public async Task JoinNewAccountAsync(DrawingParticipant participant, Guid newUserId, CancellationToken ct = default)
    {
        participant.UserId = newUserId;
        participant.Status = DrawingParticipantStatus.Joined;
        participant.AnsweredAt = access.Now;
        await drawings.UpdateParticipantAsync(participant, ct);
        await groups.AddMemberAsync(participant.GroupId, newUserId, GroupMemberRole.Member, ct);
    }

    /// <summary>
    /// Declines an invitation, or leaves after joining. After the draw the
    /// assignments stay; the organizer and the leaver's Secret Santa are told.
    /// </summary>
    public async Task DeclineAsync(Guid userId, Guid drawingId, CancellationToken ct = default)
    {
        var state = await access.LoadVisibleAsync(userId, drawingId, ct);
        var me = state.ParticipantOf(userId);
        if (me is null || me.Status == DrawingParticipantStatus.Declined)
            throw DrawingAccess.NotFound();
        await DeclineParticipantAsync(state, me, ct);
    }

    /// <summary>
    /// Declines through the personal link, signed in or not: someone invited by
    /// email can say no without making an account. A link that belongs to an
    /// account works only for that account.
    /// </summary>
    public async Task DeclineWithCodeAsync(Guid? userId, InviteTarget target, CancellationToken ct = default)
    {
        var participant = target.Participant ?? throw new InvitationNotFoundException();
        if (participant.UserId is { } owner && owner != userId)
            throw new InvitationNotFoundException();
        var state = await drawings.GetAsync(participant.GroupId, ct) ?? throw new InvitationNotFoundException();
        await DeclineParticipantAsync(state, participant, ct);
    }

    private async Task DeclineParticipantAsync(DrawingState state, DrawingParticipant participant, CancellationToken ct)
    {
        var drawingId = state.Group.Id;
        var names = state.Drawing.IsDrawn ? await DrawingNames.LoadAsync(groups, drawingId, ct) : null;
        participant.Status = DrawingParticipantStatus.Declined;
        participant.AnsweredAt = access.Now;
        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            await drawings.UpdateParticipantAsync(participant, innerCt);
            if (participant.UserId is { } memberId)
                await groups.RemoveMemberAsync(drawingId, memberId, innerCt);
        }, ct);

        if (names is not null && state.Drawing.IsActive(access.Today))
            await TellAboutLeavingAsync(state, participant, names, ct);
    }

    private async Task JoinAsUserAsync(DrawingParticipant participant, Guid userId, CancellationToken ct)
    {
        participant.UserId = userId;
        participant.Status = DrawingParticipantStatus.Joined;
        participant.AnsweredAt = access.Now;
        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            await drawings.UpdateParticipantAsync(participant, innerCt);
            if (!await groups.IsMemberAsync(participant.GroupId, userId, innerCt))
                await groups.AddMemberAsync(participant.GroupId, userId, GroupMemberRole.Member, innerCt);
        }, ct);
    }

    private async Task TellAboutLeavingAsync(DrawingState state, DrawingParticipant leaver,
        IReadOnlyDictionary<Guid, string> names, CancellationToken ct)
    {
        var byId = state.Participants.ToDictionary(p => p.Id);
        var leaverName = leaver.UserId is { } leaverId && names.TryGetValue(leaverId, out var n) ? n : leaver.Name;
        var organizer = state.Drawing.OrganizerId is { } organizerId ? await users.GetByIdAsync(organizerId, ct) : null;

        // The one who drew the leaver needn't buy a gift.
        var toLeaver = await drawings.GetAssignmentByReceiverAsync(leaver.Id, ct);
        if (toLeaver is not null && byId.TryGetValue(toLeaver.GiverParticipantId, out var giver)
            && giver.Status != DrawingParticipantStatus.Declined)
            await emails.SendNoGiftNeededAsync(state.Group, organizer, giver, leaverName, ct);

        // The one the leaver drew now has no Secret Santa: the organizer is told who.
        if (organizer is not null)
        {
            var fromLeaver = await drawings.GetAssignmentByGiverAsync(leaver.Id, ct);
            var orphan = fromLeaver is not null && byId.TryGetValue(fromLeaver.ReceiverParticipantId, out var receiver)
                && receiver.Status != DrawingParticipantStatus.Declined
                ? DrawingNames.Of(receiver, names)
                : null;
            await emails.SendLeftAfterDrawAsync(state.Group, organizer, leaverName, orphan, ct);
        }
    }
}
