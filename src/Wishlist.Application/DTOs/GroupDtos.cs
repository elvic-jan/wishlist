using Wishlist.Domain.Enums;

namespace Wishlist.Application.DTOs;

public record CreateGroupRequest(string Name);
public record GroupDto(Guid Id, string Name, DateTime CreatedAt);
public record GroupMemberDto(Guid UserId, string DisplayName, GroupMemberRole Role);
public record InvitationDto(string InviteUrl, DateTime ExpiresAt);
/// <param name="InviterName">Display name of the member who created the link ("Someone" if they're gone).</param>
public record InvitationStatusDto(DateTime ExpiresAt, string InviterName, string CircleName)
{
    /// <summary>The inviter's saved language, else English; for the link preview. Not sent to the browser.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string InviterLanguage { get; init; } = Localization.Languages.Default;
}
