using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wishlist.Application.DTOs;
using Wishlist.Application.UseCases.Groups;
using Wishlist.Application.UseCases.Wishlists;

namespace Wishlist.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class GroupsController(
    GetUserGroupsUseCase getUserGroups,
    CreateGroupUseCase createGroup,
    GenerateGroupInvitationUseCase generateInvitation,
    CheckInvitationUseCase checkInvitation,
    JoinGroupViaInvitationUseCase joinGroup,
    InviteCodeGuard inviteCodeGuard,
    ListGroupMembersUseCase listMembers,
    LeaveGroupUseCase leaveGroup,
    GetInviteCandidatesUseCase inviteCandidates,
    InviteFromCirclesUseCase inviteFromCircles,
    GetMyCircleInvitationsUseCase myCircleInvitations,
    AnswerCircleInvitationUseCase answerCircleInvitation) : ControllerBase
{
    [HttpGet("groups/me")]
    public async Task<IActionResult> MyGroups(CancellationToken ct)
    {
        var result = await getUserGroups.ExecuteAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    [HttpPost("groups")]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        var result = await createGroup.ExecuteAsync(User.GetUserId(), request, ct);
        return Ok(result);
    }

    [HttpPost("groups/{groupId:guid}/invitations")]
    public async Task<IActionResult> GenerateInvitation(Guid groupId, CancellationToken ct)
    {
        var result = await generateInvitation.ExecuteAsync(User.GetUserId(), groupId, ct);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("invitations/{code}")]
    public Task<IActionResult> Check(string code, CancellationToken ct) =>
        inviteCodeGuard.RunAsync(HttpContext, async () => Ok(await checkInvitation.ExecuteAsync(code, ct)));

    [HttpPost("invitations/{code}/join")]
    public Task<IActionResult> Join(string code, CancellationToken ct) =>
        inviteCodeGuard.RunAsync(HttpContext, async () =>
        {
            await joinGroup.ExecuteAsync(User.GetUserId(), code, ct);
            return Ok();
        });

    [HttpGet("groups/{groupId:guid}/members")]
    public async Task<IActionResult> ListMembers(Guid groupId, CancellationToken ct)
    {
        var result = await listMembers.ExecuteAsync(User.GetUserId(), groupId, ct);
        return Ok(result);
    }

    [HttpDelete("groups/{groupId:guid}/members/me")]
    public async Task<IActionResult> Leave(Guid groupId, CancellationToken ct)
    {
        await leaveGroup.ExecuteAsync(User.GetUserId(), groupId, ct);
        return NoContent();
    }

    // --- Inviting people you know from your other circles ---

    [HttpGet("groups/{groupId:guid}/invite-candidates")]
    public async Task<IActionResult> InviteCandidates(Guid groupId, CancellationToken ct) =>
        Ok(await inviteCandidates.ExecuteAsync(User.GetUserId(), groupId, ct));

    [HttpPost("groups/{groupId:guid}/circle-invitations")]
    public async Task<IActionResult> InviteFromCircles(Guid groupId, [FromBody] InviteFromCirclesRequest request, CancellationToken ct) =>
        Ok(await inviteFromCircles.ExecuteAsync(User.GetUserId(), groupId, request, ct));

    [HttpGet("circle-invitations/me")]
    public async Task<IActionResult> MyCircleInvitations(CancellationToken ct) =>
        Ok(await myCircleInvitations.ExecuteAsync(User.GetUserId(), ct));

    [HttpPost("circle-invitations/{invitationId:guid}/accept")]
    public async Task<IActionResult> AcceptCircleInvitation(Guid invitationId, CancellationToken ct) =>
        Ok(new { groupId = await answerCircleInvitation.JoinAsync(User.GetUserId(), invitationId, ct) });

    [HttpPost("circle-invitations/{invitationId:guid}/decline")]
    public async Task<IActionResult> DeclineCircleInvitation(Guid invitationId, CancellationToken ct)
    {
        await answerCircleInvitation.DeclineAsync(User.GetUserId(), invitationId, ct);
        return NoContent();
    }
}
