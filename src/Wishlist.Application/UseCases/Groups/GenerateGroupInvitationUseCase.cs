using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Groups;

public class GenerateGroupInvitationUseCase(IGroupRepository groups, IInvitationRepository invitations)
{
    public async Task<InvitationDto> ExecuteAsync(Guid userId, Guid groupId, string baseUrl, CancellationToken ct = default)
    {
        if (!await groups.IsMemberAsync(groupId, userId, ct))
            throw new ForbiddenException("You must be a member of this circle to generate an invitation.");

        var invitation = await invitations.CreateAsync(groupId, userId, ct);
        var url = $"{baseUrl}/join/{invitation.InviteCode}";

        return new InvitationDto(url, invitation.ExpiresAt);
    }
}
