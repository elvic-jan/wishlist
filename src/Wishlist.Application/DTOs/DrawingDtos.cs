namespace Wishlist.Application.DTOs;

// ── Requests ────────────────────────────────────────────────────────────────

/// <param name="IncludeMe">The organizer takes part (ticked by default).</param>
public record CreateDrawingRequest(
    string? Title,
    DateOnly? EndDate,
    string? Budget,
    bool IncludeMe = true,
    AddDrawingParticipantsRequest? Participants = null);

/// <summary>People to add: whole circles (minus some members), circle-mates, and names with email addresses.</summary>
public record AddDrawingParticipantsRequest(
    IReadOnlyList<DrawingCircleSelection>? Circles = null,
    IReadOnlyList<Guid>? UserIds = null,
    IReadOnlyList<DrawingEmailInvite>? Invites = null);

public record DrawingCircleSelection(Guid CircleId, IReadOnlyList<Guid>? ExcludedUserIds = null);

public record DrawingEmailInvite(string? Name, string? Email);

public record DrawingExclusionPair(Guid First, Guid Second);

public record SetDrawingExclusionsRequest(IReadOnlyList<DrawingExclusionPair>? Pairs);

/// <param name="ConfirmUnanswered">The organizer saw who hasn't answered and wants them in the draw.</param>
public record DrawNamesRequest(bool ConfirmUnanswered = false);

// ── Responses ───────────────────────────────────────────────────────────────

/// <summary>Statuses as sent to the app.</summary>
public static class DrawingStatuses
{
    public const string Invited = "invited";
    public const string Joined = "joined";
    public const string Declined = "declined";

    public static string Of(Domain.Enums.DrawingParticipantStatus status) => status switch
    {
        Domain.Enums.DrawingParticipantStatus.Joined => Joined,
        Domain.Enums.DrawingParticipantStatus.Declined => Declined,
        _ => Invited,
    };
}

/// <summary>A drawing as its organizer or a participant sees it. Never contains anyone else's recipient.</summary>
public record DrawingDto(
    Guid Id,
    string Title,
    DateOnly EndDate,
    string? Budget,
    string OrganizerName,
    bool IsOrganizer,
    bool Drawn,
    bool Cancelled,
    bool Ended,
    Guid? MyParticipantId,
    string? MyStatus,
    IReadOnlyList<DrawingParticipantDto> Participants,
    IReadOnlyList<DrawingExclusionPair> Exclusions,
    DrawingRecipientDto? MyRecipient);

/// <param name="UserId">Set only when the viewer may open this person's wishlist.</param>
/// <param name="Email">The typed address; for the organizer only, until the person joins.</param>
public record DrawingParticipantDto(
    Guid Id,
    string Name,
    string Status,
    Guid? UserId,
    string? Email,
    bool IsMe,
    bool CanJoinForThem);

/// <param name="UserId">Set only when the viewer may open the recipient's wishlist.</param>
public record DrawingRecipientDto(Guid DrawingId, string DrawingTitle, string Name, Guid? UserId, bool Joined);

/// <summary>An unanswered invitation, for the Join / Decline card.</summary>
public record DrawingInvitationDto(
    Guid DrawingId,
    Guid ParticipantId,
    string Title,
    string OrganizerName,
    DateOnly EndDate,
    string? Budget,
    bool Drawn);

/// <summary>A drawing in the circles list.</summary>
public record DrawingSummaryDto(DateOnly EndDate, bool IsOrganizer, string? MyStatus, bool Drawn, bool Ended, bool Cancelled);
