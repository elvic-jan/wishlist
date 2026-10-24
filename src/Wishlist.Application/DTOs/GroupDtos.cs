using Wishlist.Domain.Enums;

namespace Wishlist.Application.DTOs;

public record CreateGroupRequest(string Name);
public record GroupDto(Guid Id, string Name, DateTime CreatedAt);
public record GroupMemberDto(Guid UserId, string DisplayName, GroupMemberRole Role);
public record InvitationDto(string InviteUrl, DateTime ExpiresAt);
public record InvitationStatusDto(DateTime ExpiresAt);
