using Wishlist.Domain.Enums;

namespace Wishlist.Domain.Entities;

/// <summary>
/// A Secret Santa drawing: a temporary circle (its <see cref="Group"/>, kind
/// Drawing) with an end date, participants, exclusions and secret assignments.
/// Visible until a month after its end date; a cancelled one is hidden from
/// participants at once and from the organizer a month later. The monthly job
/// then deletes it.
/// </summary>
public class Drawing
{
    /// <summary>How long a drawing stays visible after its end date, and a cancelled one stays undoable.</summary>
    public const int VisibleMonths = 1;

    public Guid GroupId { get; set; }

    /// <summary>Null once the organizer's account is gone.</summary>
    public Guid? OrganizerId { get; set; }

    public DateOnly EndDate { get; set; }
    public string? Budget { get; set; }
    public DateTime? DrawnAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsDrawn => DrawnAt is not null;
    public bool IsCancelled => CancelledAt is not null;

    /// <summary>The end date has passed.</summary>
    public bool IsEnded(DateOnly today) => today > EndDate;

    /// <summary>More than a month past the end date: shown to nobody, waiting for cleanup.</summary>
    public bool IsExpired(DateOnly today) => EndDate < today.AddMonths(-VisibleMonths);

    /// <summary>Connects its members (wishlists, reservations, buddies): not cancelled and not expired.</summary>
    public bool IsActive(DateOnly today) => !IsCancelled && !IsExpired(today);

    /// <summary>The organizer still sees it: not expired, and cancelled less than a month ago.</summary>
    public bool IsVisibleToOrganizer(DateTime now) =>
        !IsExpired(DateOnly.FromDateTime(now)) && (CancelledAt is not { } at || at.AddMonths(VisibleMonths) > now);

    /// <summary>The monthly job may delete it.</summary>
    public bool IsDueForCleanup(DateTime now) =>
        IsExpired(DateOnly.FromDateTime(now)) || CancelledAt is { } at && at.AddMonths(VisibleMonths) <= now;
}

/// <summary>
/// Someone in a drawing: an app user, or a name and email address invited to
/// join. Only Joined participants are members of the drawing's circle.
/// </summary>
public class DrawingParticipant
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }

    /// <summary>The account; null for an invited address until someone joins with its link.</summary>
    public Guid? UserId { get; set; }

    /// <summary>The name shown until they join (typed by the organizer, or the user's name when added).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The address the organizer typed; null for people picked from their circles.</summary>
    public string? Email { get; set; }

    public DrawingParticipantStatus Status { get; set; }

    /// <summary>The personal code in the participant's join link (32 hex characters).</summary>
    public string InviteCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }

    /// <summary>Added by typing an email address (shown by the typed name until they join).</summary>
    public bool IsInvitedByEmail => Email is not null;
}

/// <summary>Two participants who may not draw each other. Stored with the smaller id first.</summary>
public class DrawingExclusion
{
    public Guid GroupId { get; set; }
    public Guid FirstParticipantId { get; set; }
    public Guid SecondParticipantId { get; set; }
}

/// <summary>The giver buys for the receiver. Secret: only the giver may read it.</summary>
public class DrawingAssignment
{
    public Guid GroupId { get; set; }
    public Guid GiverParticipantId { get; set; }
    public Guid ReceiverParticipantId { get; set; }
}
