using System.Net;
using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Groups;

public record InviteFromCirclesRequest(IReadOnlyList<Guid>? UserIds);
public record InviteFromCirclesResult(int Invited, int Skipped);

/// <summary>The people a member could invite into a circle, grouped by the member's other circles.</summary>
public class GetInviteCandidatesUseCase(IGroupRepository groups, ICircleInvitationRepository invitations)
{
    public async Task<IReadOnlyList<InviteCandidateCircle>> ExecuteAsync(Guid userId, Guid groupId, CancellationToken ct = default)
    {
        if (!await groups.IsMemberAsync(groupId, userId, ct))
            throw new ForbiddenException("You are not a member of this circle.");

        return await invitations.GetCandidatesAsync(groupId, userId, ct);
    }
}

/// <summary>A member invites people they know from their other circles; each must accept.</summary>
public class InviteFromCirclesUseCase(
    IGroupRepository groups,
    ICircleInvitationRepository invitations,
    IUserRepository users,
    CircleInvitationEmails emails,
    TimeProvider time)
{
    public const int MaxPerRequest = 100;

    public async Task<InviteFromCirclesResult> ExecuteAsync(Guid userId, Guid groupId, InviteFromCirclesRequest request, CancellationToken ct = default)
    {
        var ids = (request.UserIds ?? []).Distinct().ToList();
        if (ids.Count == 0 || ids.Count > MaxPerRequest)
            throw new DomainException($"Pick between 1 and {MaxPerRequest} people.");

        if (!await groups.IsMemberAsync(groupId, userId, ct))
            throw new ForbiddenException("You are not a member of this circle.");

        var allowed = await invitations.GetCircleMatesAsync(userId, ids.Where(id => id != userId).ToList(), ct);
        if (allowed.Count == 0)
            throw new ForbiddenException("You can only invite people from your circles.");

        var members = await invitations.GetMembersAsync(groupId, allowed.ToList(), ct);
        var alreadyInvited = await invitations.GetInvitedAsync(groupId, allowed.ToList(), ct);
        var toInvite = allowed.Where(id => !members.Contains(id) && !alreadyInvited.Contains(id)).ToList();

        var added = toInvite.Count == 0
            ? []
            : await invitations.CreateAsync(groupId, userId, toInvite, time.GetUtcNow().UtcDateTime, ct);

        if (added.Count > 0)
        {
            var circle = await groups.GetByIdAsync(groupId, ct);
            var inviter = await users.GetByIdAsync(userId, ct);
            foreach (var inviteeId in added)
            {
                var invitee = await users.GetByIdAsync(inviteeId, ct);
                if (invitee is not null && circle is not null && inviter is not null)
                    await emails.SendInvitationAsync(invitee, inviter, circle, ct);
            }
        }

        return new InviteFromCirclesResult(added.Count, ids.Count - added.Count);
    }
}

public class GetMyCircleInvitationsUseCase(ICircleInvitationRepository invitations)
{
    public Task<IReadOnlyList<MyCircleInvitation>> ExecuteAsync(Guid userId, CancellationToken ct = default) =>
        invitations.GetForInviteeAsync(userId, ct);
}

/// <summary>The invitee joins or declines. Either answer closes the invitation.</summary>
public class AnswerCircleInvitationUseCase(
    ICircleInvitationRepository invitations,
    IGroupRepository groups,
    IUnitOfWork unitOfWork)
{
    /// <summary>Returns the circle's id, so the client can open it.</summary>
    public async Task<Guid> JoinAsync(Guid userId, Guid invitationId, CancellationToken ct = default)
    {
        var invitation = await GetOwnAsync(userId, invitationId, ct);

        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            if (!await groups.IsMemberAsync(invitation.GroupId, userId, innerCt))
                await groups.AddMemberAsync(invitation.GroupId, userId, GroupMemberRole.Member, innerCt);
            await invitations.DeleteAsync(invitation.Id, innerCt);
        }, ct);

        return invitation.GroupId;
    }

    public async Task DeclineAsync(Guid userId, Guid invitationId, CancellationToken ct = default)
    {
        var invitation = await GetOwnAsync(userId, invitationId, ct);
        await invitations.DeleteAsync(invitation.Id, ct);
    }

    /// <summary>Someone else's invitation looks the same as a missing one.</summary>
    private async Task<CircleInvitation> GetOwnAsync(Guid userId, Guid invitationId, CancellationToken ct)
    {
        var invitation = await invitations.GetByIdAsync(invitationId, ct);
        if (invitation is null || invitation.InviteeId != userId)
            throw new NotFoundException("Invitation not found.");
        return invitation;
    }
}

/// <summary>Tells an existing user they've been invited to a circle. Best-effort.</summary>
public class CircleInvitationEmails(IEmailSender emailSender, IAppUrlProvider appUrls, ILogger<CircleInvitationEmails> logger)
{
    public async Task SendInvitationAsync(User invitee, User inviter, Group circle, CancellationToken ct = default)
    {
        var link = $"{appUrls.PublicBaseUrl.TrimEnd('/')}/";
        var message = new EmailMessage(
            To: invitee.Email,
            Subject: $"{inviter.DisplayName} invited you to join {circle.Name}",
            PlainTextBody:
                $"Hi {invitee.DisplayName},\r\n\r\n" +
                $"{inviter.DisplayName} invited you to join the circle \"{circle.Name}\" on Kadoozle. " +
                "Open Kadoozle to join or decline:\r\n" +
                $"{link}\r\n\r\n" +
                "Thanks,\r\nKadoozle",
            HtmlBody:
                $"<p>Hi {Html(invitee.DisplayName)},</p>" +
                $"<p>{Html(inviter.DisplayName)} invited you to join the circle <strong>{Html(circle.Name)}</strong> on Kadoozle.</p>" +
                $"<p><a href=\"{link}\">Open Kadoozle to join or decline</a></p>" +
                "<p>Thanks,<br/>Kadoozle</p>");

        try
        {
            await emailSender.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send the circle invitation email to user {UserId}.", invitee.Id);
        }
    }

    private static string Html(string text) => WebUtility.HtmlEncode(text);
}
