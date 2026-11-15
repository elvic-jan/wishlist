using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Drawings;

/// <summary>Any signed-in user creates a drawing and becomes its organizer; the people are invited at once.</summary>
public class CreateDrawingUseCase(
    IDrawingRepository drawings,
    IGroupRepository groups,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    DrawingParticipantPicker picker,
    DrawingEmails emails,
    DrawingAccess access)
{
    public async Task<Guid> ExecuteAsync(Guid userId, CreateDrawingRequest request, CancellationToken ct = default)
    {
        var title = request.Title?.Trim();
        if (string.IsNullOrEmpty(title))
            throw new DomainException("Title cannot be empty.");
        InputLimits.Check(title, InputLimits.DrawingTitle, "Title");
        if (request.EndDate is not { } endDate)
            throw new DomainException("Choose an end date.");
        if (endDate < access.Today)
            throw new DomainException("The end date can't be in the past.");
        var budget = string.IsNullOrWhiteSpace(request.Budget) ? null : request.Budget.Trim();
        InputLimits.Check(budget, InputLimits.DrawingBudget, "Budget");

        var organizer = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User not found.");
        var now = access.Now;

        Group group = null!;
        IReadOnlyList<DrawingParticipant> invited = [];
        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            group = await drawings.CreateAsync(title, userId, endDate, budget, now, innerCt);
            var existing = new List<DrawingParticipant>();
            if (request.IncludeMe)
            {
                var me = new DrawingParticipant
                {
                    Id = Guid.NewGuid(),
                    GroupId = group.Id,
                    UserId = userId,
                    Name = organizer.DisplayName,
                    Status = DrawingParticipantStatus.Joined,
                    InviteCode = Guid.NewGuid().ToString("N"),
                    CreatedAt = now,
                    AnsweredAt = now,
                };
                await drawings.AddParticipantsAsync([me], innerCt);
                await groups.AddMemberAsync(group.Id, userId, GroupMemberRole.Member, innerCt);
                existing.Add(me);
            }
            invited = await picker.AddAsync(group.Id, userId, existing, request.Participants, now, innerCt);
        }, ct);

        var drawing = (await drawings.GetAsync(group.Id, ct))!.Drawing;
        foreach (var participant in invited)
            await emails.SendInvitationAsync(group, drawing, organizer, participant, ct);
        return group.Id;
    }
}

/// <summary>The organizer adds, removes and pairs people until the names are drawn.</summary>
public class ManageDrawingParticipantsUseCase(
    IDrawingRepository drawings,
    IGroupRepository groups,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    DrawingParticipantPicker picker,
    DrawingEmails emails,
    DrawingAccess access)
{
    public async Task<int> AddAsync(Guid userId, Guid drawingId, AddDrawingParticipantsRequest request, CancellationToken ct = default)
    {
        var state = await LoadUndrawnAsync(userId, drawingId, ct);
        IReadOnlyList<DrawingParticipant> invited = [];
        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
            invited = await picker.AddAsync(drawingId, userId, state.Participants, request, access.Now, innerCt), ct);

        var organizer = await users.GetByIdAsync(userId, ct);
        foreach (var participant in invited)
            await emails.SendInvitationAsync(state.Group, state.Drawing, organizer, participant, ct);
        return invited.Count;
    }

    /// <summary>Removes someone before the draw, without telling them.</summary>
    public async Task RemoveAsync(Guid userId, Guid drawingId, Guid participantId, CancellationToken ct = default)
    {
        var state = await LoadUndrawnAsync(userId, drawingId, ct);
        var participant = state.Participants.FirstOrDefault(p => p.Id == participantId)
            ?? throw new NotFoundException("Participant not found.");

        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            if (participant.UserId is { } memberId)
                await groups.RemoveMemberAsync(drawingId, memberId, innerCt);
            await drawings.RemoveParticipantAsync(participant.Id, innerCt);
        }, ct);
    }

    /// <summary>Replaces the exclusions. Each pair works both ways; duplicates are merged.</summary>
    public async Task SetExclusionsAsync(Guid userId, Guid drawingId, SetDrawingExclusionsRequest request, CancellationToken ct = default)
    {
        var state = await LoadUndrawnAsync(userId, drawingId, ct);
        var ids = state.Participants.Select(p => p.Id).ToHashSet();
        var exclusions = new Dictionary<(Guid, Guid), DrawingExclusion>();
        foreach (var pair in request.Pairs ?? [])
        {
            if (pair.First == pair.Second || !ids.Contains(pair.First) || !ids.Contains(pair.Second))
                throw new DomainException("An exclusion needs two different people from this drawing.");
            var (first, second) = pair.First.CompareTo(pair.Second) < 0 ? (pair.First, pair.Second) : (pair.Second, pair.First);
            exclusions[(first, second)] = new DrawingExclusion
            {
                GroupId = drawingId,
                FirstParticipantId = first,
                SecondParticipantId = second,
            };
        }
        await drawings.SetExclusionsAsync(drawingId, exclusions.Values.ToList(), ct);
    }

    private async Task<DrawingState> LoadUndrawnAsync(Guid userId, Guid drawingId, CancellationToken ct)
    {
        var state = await access.LoadAsOrganizerAsync(userId, drawingId, ct);
        if (!state.Drawing.IsActive(access.Today))
            throw new ConflictException("This drawing is cancelled.");
        if (state.Drawing.IsDrawn)
            throw new ConflictException("The names have already been drawn.");
        return state;
    }
}

/// <summary>The organizer draws the names: one loop through everyone who hasn't declined.</summary>
public class DrawNamesUseCase(
    IDrawingRepository drawings,
    IGroupRepository groups,
    IUserRepository users,
    DrawingEmails emails,
    DrawingAccess access)
{
    public async Task ExecuteAsync(Guid userId, Guid drawingId, DrawNamesRequest request, CancellationToken ct = default)
    {
        var state = await access.LoadAsOrganizerAsync(userId, drawingId, ct);
        if (!state.Drawing.IsActive(access.Today))
            throw new ConflictException("This drawing is cancelled.");
        if (state.Drawing.IsDrawn)
            throw new ConflictException("The names have already been drawn.");

        var included = state.Participants.Where(p => p.Status != DrawingParticipantStatus.Declined).ToList();
        if (included.Count < SecretSanta.MinimumParticipants)
            throw new DomainException("At least {0} people need to take part to draw names.", SecretSanta.MinimumParticipants);
        if (!request.ConfirmUnanswered && included.Any(p => p.Status == DrawingParticipantStatus.Invited))
            throw new ConflictException("Some people haven't answered yet. Confirm to include them in the draw.");

        var includedIds = included.Select(p => p.Id).ToHashSet();
        var exclusions = state.Exclusions
            .Where(x => includedIds.Contains(x.FirstParticipantId) && includedIds.Contains(x.SecondParticipantId))
            .Select(x => (x.FirstParticipantId, x.SecondParticipantId))
            .ToList();
        var loop = SecretSanta.Draw(includedIds, exclusions)
            ?? throw new DomainException("No draw is possible with these exclusions. Remove some exclusions and try again.");

        var assignments = loop.Select(pair => new DrawingAssignment
        {
            GroupId = drawingId,
            GiverParticipantId = pair.Key,
            ReceiverParticipantId = pair.Value,
        }).ToList();
        if (!await drawings.SaveDrawAsync(drawingId, assignments, access.Now, ct))
            throw new ConflictException("The names have already been drawn.");

        var organizer = await users.GetByIdAsync(userId, ct);
        var names = await DrawingNames.LoadAsync(groups, drawingId, ct);
        var byId = included.ToDictionary(p => p.Id);
        foreach (var (giverId, receiverId) in loop)
        {
            var receiver = byId[receiverId];
            await emails.SendRecipientAsync(state.Group, state.Drawing, organizer, byId[giverId], receiver,
                DrawingNames.Of(receiver, names), ct);
        }
    }
}

/// <summary>The organizer cancels a drawing, or undoes that until it is cleaned up.</summary>
public class CancelDrawingUseCase(IDrawingRepository drawings, IUserRepository users, DrawingEmails emails, DrawingAccess access)
{
    public async Task CancelAsync(Guid userId, Guid drawingId, CancellationToken ct = default)
    {
        var state = await access.LoadAsOrganizerAsync(userId, drawingId, ct);
        if (state.Drawing.IsCancelled) return;

        await drawings.SetCancelledAsync(drawingId, access.Now, ct);
        await NotifyAsync(state, userId, emails.SendCancelledAsync, ct);
    }

    public async Task UndoAsync(Guid userId, Guid drawingId, CancellationToken ct = default)
    {
        var state = await access.LoadAsOrganizerAsync(userId, drawingId, ct);
        if (!state.Drawing.IsCancelled) return;

        await drawings.SetCancelledAsync(drawingId, null, ct);
        await NotifyAsync(state, userId, emails.SendBackOnAsync, ct);
    }

    /// <summary>Everyone who hasn't declined, except the organizer, who did it.</summary>
    private async Task NotifyAsync(DrawingState state, Guid organizerId,
        Func<Group, User?, DrawingParticipant, CancellationToken, Task> send, CancellationToken ct)
    {
        var organizer = await users.GetByIdAsync(organizerId, ct);
        foreach (var participant in state.Participants)
            if (participant.Status != DrawingParticipantStatus.Declined && participant.UserId != organizerId)
                await send(state.Group, organizer, participant, ct);
    }
}

/// <summary>The monthly job deletes drawings that ended or were cancelled more than a month ago.</summary>
public class CleanUpDrawingsUseCase(IDrawingRepository drawings, TimeProvider time)
{
    public Task<int> ExecuteAsync(CancellationToken ct = default) =>
        drawings.DeleteDueAsync(time.GetUtcNow().UtcDateTime, ct);
}

/// <summary>Display names: a participant who joined shows their account's name, others the name they were added with.</summary>
public static class DrawingNames
{
    public static async Task<IReadOnlyDictionary<Guid, string>> LoadAsync(IGroupRepository groups, Guid drawingId, CancellationToken ct) =>
        (await groups.GetMembersAsync(drawingId, ct)).ToDictionary(m => m.UserId, m => m.DisplayName);

    public static string Of(DrawingParticipant participant, IReadOnlyDictionary<Guid, string> memberNames) =>
        participant.Status == DrawingParticipantStatus.Joined && participant.UserId is { } id && memberNames.TryGetValue(id, out var name)
            ? name
            : participant.Name;
}
