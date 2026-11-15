using Wishlist.Domain.Enums;

namespace Wishlist.Application.DTOs;

public record CreateGroupRequest(string Name);
/// <param name="Kind">"circle" or "drawing".</param>
/// <param name="Drawing">Set for a Secret Santa drawing.</param>
public record GroupDto(Guid Id, string Name, DateTime CreatedAt, string Kind = "circle", DrawingSummaryDto? Drawing = null);
public record GroupMemberDto(Guid UserId, string DisplayName, GroupMemberRole Role);
public record InvitationDto(string InviteUrl, DateTime ExpiresAt);
/// <param name="InviterName">Display name of the member who created the link ("Someone" if they're gone).</param>
public record InvitationStatusDto(DateTime ExpiresAt, string InviterName, string CircleName)
{
    /// <summary>"circle", or "drawing" for a personal Secret Santa invitation.</summary>
    public string Kind { get; init; } = "circle";

    /// <summary>For a drawing invitation: the name and address the organizer typed, to fill in the sign-up form.</summary>
    public string? Name { get; init; }
    public string? Email { get; init; }

    /// <summary>The drawing invitation belongs to an existing account: sign in to answer it.</summary>
    public bool SignInRequired { get; init; }

    /// <summary>The inviter's saved language, else English; for the link preview. Not sent to the browser.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string InviterLanguage { get; init; } = Localization.Languages.Default;
}
