using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Groups;

public class GenerateGroupInvitationUseCase(IGroupRepository groups, IInvitationRepository invitations, IAppUrlProvider appUrls)
{
    /// <summary>The link uses the configured address, never the request's Host header.</summary>
    public async Task<InvitationDto> ExecuteAsync(Guid userId, Guid groupId, CancellationToken ct = default)
    {
        if (!await groups.IsMemberAsync(groupId, userId, ct))
            throw new ForbiddenException("You must be a member of this circle to generate an invitation.");

        var invitation = await invitations.CreateAsync(groupId, userId, ct);
        var url = $"{appUrls.PublicBaseUrl.TrimEnd('/')}/join/{invitation.InviteCode}";

        return new InvitationDto(url, invitation.ExpiresAt);
    }
}
