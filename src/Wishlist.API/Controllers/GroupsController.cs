using System.Security.Claims;
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
    JoinGroupViaInvitationUseCase joinGroup,
    ListGroupMembersUseCase listMembers,
    LeaveGroupUseCase leaveGroup) : ControllerBase
{
    [HttpGet("groups/me")]
    public async Task<IActionResult> MyGroups(CancellationToken ct)
    {
        var result = await getUserGroups.ExecuteAsync(GetUserId(), ct);
        return Ok(result);
    }

    [HttpPost("groups")]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        var result = await createGroup.ExecuteAsync(GetUserId(), request, ct);
        return Ok(result);
    }

    [HttpPost("groups/{groupId:guid}/invitations")]
    public async Task<IActionResult> GenerateInvitation(Guid groupId, CancellationToken ct)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await generateInvitation.ExecuteAsync(GetUserId(), groupId, baseUrl, ct);
        return Ok(result);
    }

    [HttpPost("invitations/{code}/join")]
    public async Task<IActionResult> Join(string code, CancellationToken ct)
    {
        await joinGroup.ExecuteAsync(GetUserId(), code, ct);
        return Ok();
    }

    [HttpGet("groups/{groupId:guid}/members")]
    public async Task<IActionResult> ListMembers(Guid groupId, CancellationToken ct)
    {
        var result = await listMembers.ExecuteAsync(GetUserId(), groupId, ct);
        return Ok(result);
    }

    [HttpDelete("groups/{groupId:guid}/members/me")]
    public async Task<IActionResult> Leave(Guid groupId, CancellationToken ct)
    {
        await leaveGroup.ExecuteAsync(GetUserId(), groupId, ct);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim missing."));
}
