using Wishlist.Domain.Enums;

namespace Wishlist.Domain.Entities;

public class GroupMember
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public GroupMemberRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
