using Wishlist.Domain.Entities;

namespace Wishlist.Application.Interfaces;

public interface IInvitationRepository
{
    Task<GroupInvitation> CreateAsync(Guid groupId, Guid createdByUserId, CancellationToken ct = default);
    Task<GroupInvitation?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task MarkUsedAsync(Guid invitationId, CancellationToken ct = default);
}
