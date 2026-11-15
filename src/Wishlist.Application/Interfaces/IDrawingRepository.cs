using Wishlist.Domain.Entities;

namespace Wishlist.Application.Interfaces;

/// <summary>A drawing with its circle, participants and exclusions; never its assignments.</summary>
public record DrawingState(
    Group Group,
    Drawing Drawing,
    IReadOnlyList<DrawingParticipant> Participants,
    IReadOnlyList<DrawingExclusion> Exclusions)
{
    public DrawingParticipant? ParticipantOf(Guid userId) => Participants.FirstOrDefault(p => p.UserId == userId);
}

/// <summary>A drawing as one of the user's circles: their part in it.</summary>
public record DrawingForUser(Group Group, Drawing Drawing, DrawingParticipant? Me, bool IsOrganizer);

/// <summary>Someone the user drew, in a drawing that is still active.</summary>
public record DrawnRecipient(Guid DrawingId, string DrawingTitle, Guid ReceiverParticipantId, string ReceiverName,
    Guid? ReceiverUserId, bool ReceiverJoined, bool ReceiverInvitedByEmail, bool GiverJoined);

public interface IDrawingRepository
{
    /// <summary>Creates the drawing's circle (kind Drawing) and its drawing row.</summary>
    Task<Group> CreateAsync(string title, Guid organizerId, DateOnly endDate, string? budget, DateTime now, CancellationToken ct = default);

    Task<DrawingState?> GetAsync(Guid groupId, CancellationToken ct = default);

    Task<DrawingParticipant?> GetParticipantByCodeAsync(string code, CancellationToken ct = default);

    Task AddParticipantsAsync(IReadOnlyCollection<DrawingParticipant> participants, CancellationToken ct = default);

    /// <summary>Saves a participant's status, account and answer time.</summary>
    Task UpdateParticipantAsync(DrawingParticipant participant, CancellationToken ct = default);

    /// <summary>Deletes the participant and the exclusions they are in.</summary>
    Task RemoveParticipantAsync(Guid participantId, CancellationToken ct = default);

    Task SetExclusionsAsync(Guid groupId, IReadOnlyCollection<DrawingExclusion> exclusions, CancellationToken ct = default);

    /// <summary>
    /// Marks the drawing drawn and saves the assignments, unless it was drawn
    /// already (then nothing changes and false is returned).
    /// </summary>
    Task<bool> SaveDrawAsync(Guid groupId, IReadOnlyCollection<DrawingAssignment> assignments, DateTime now, CancellationToken ct = default);

    Task<DrawingAssignment?> GetAssignmentByGiverAsync(Guid giverParticipantId, CancellationToken ct = default);
    Task<DrawingAssignment?> GetAssignmentByReceiverAsync(Guid receiverParticipantId, CancellationToken ct = default);

    /// <summary>Sets or clears the cancellation.</summary>
    Task SetCancelledAsync(Guid groupId, DateTime? cancelledAt, CancellationToken ct = default);

    /// <summary>Drawings the user organizes, or takes part in without having declined; hidden ones left out.</summary>
    Task<IReadOnlyList<DrawingForUser>> GetForUserAsync(Guid userId, DateTime now, CancellationToken ct = default);

    /// <summary>The people the user drew in active drawings in which they haven't declined.</summary>
    Task<IReadOnlyList<DrawnRecipient>> GetMyRecipientsAsync(Guid userId, DateOnly today, CancellationToken ct = default);

    /// <summary>Deletes drawings that ended or were cancelled more than a month ago. Returns how many.</summary>
    Task<int> DeleteDueAsync(DateTime now, CancellationToken ct = default);
}
