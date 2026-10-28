using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;

namespace Wishlist.Application.Interfaces;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid groupId, CancellationToken ct = default);
    Task<Group> CreateAsync(string name, Guid createdByUserId, CancellationToken ct = default);
    Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<GroupMember>> GetMembersAsync(Guid groupId, CancellationToken ct = default);
    Task AddMemberAsync(Guid groupId, Guid userId, GroupMemberRole role, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default);
    Task<int> GetMemberCountAsync(Guid groupId, CancellationToken ct = default);
    Task<GroupMemberRole?> GetMemberRoleAsync(Guid groupId, Guid userId, CancellationToken ct = default);
    Task<bool> SharesGroupAsync(Guid userId1, Guid userId2, CancellationToken ct = default);
    Task<IReadOnlyList<Group>> GetUserGroupsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>The other members of the user's circles: one row per person and circle they share with the user.</summary>
    Task<IReadOnlyList<(Guid UserId, string DisplayName, Guid GroupId, string GroupName)>> GetCircleMatesAsync(Guid userId, CancellationToken ct = default);
}
