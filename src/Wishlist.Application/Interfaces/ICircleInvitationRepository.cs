using Wishlist.Domain.Entities;

namespace Wishlist.Application.Interfaces;

public record InviteCandidate(Guid UserId, string DisplayName, bool Invited);
public record InviteCandidateCircle(Guid GroupId, string Name, IReadOnlyList<InviteCandidate> Members);
public record MyCircleInvitation(Guid Id, Guid GroupId, string CircleName, string? InvitedByName, DateTime CreatedAt);

public interface ICircleInvitationRepository
{
    /// <summary>
    /// The caller's other circles with their members, minus the caller and anyone
    /// already in <paramref name="groupId"/>; empty circles are left out. Sorted by name.
    /// </summary>
    Task<IReadOnlyList<InviteCandidateCircle>> GetCandidatesAsync(Guid groupId, Guid callerId, CancellationToken ct = default);

    /// <summary>Of <paramref name="userIds"/>, the ones that already have an open invitation to the circle.</summary>
    Task<IReadOnlySet<Guid>> GetInvitedAsync(Guid groupId, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);

    /// <summary>Of <paramref name="userIds"/>, the ones that share at least one circle with <paramref name="userId"/>.</summary>
    Task<IReadOnlySet<Guid>> GetCircleMatesAsync(Guid userId, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);

    /// <summary>Of <paramref name="userIds"/>, the ones already in the circle.</summary>
    Task<IReadOnlySet<Guid>> GetMembersAsync(Guid groupId, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default);

    /// <summary>Creates the invitations; returns the invitee ids that were actually added (a concurrent duplicate is skipped).</summary>
    Task<IReadOnlyList<Guid>> CreateAsync(Guid groupId, Guid invitedById, IReadOnlyCollection<Guid> inviteeIds, DateTime now, CancellationToken ct = default);

    Task<IReadOnlyList<MyCircleInvitation>> GetForInviteeAsync(Guid inviteeId, CancellationToken ct = default);
    Task<CircleInvitation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
