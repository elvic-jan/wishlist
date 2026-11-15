using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wishlist.Application.DTOs;
using Wishlist.Application.UseCases.Drawings;

namespace Wishlist.API.Controllers;

/// <summary>Secret Santa drawings (openspec: secret-santa).</summary>
[ApiController]
[Route("api/drawings")]
[Authorize]
public class DrawingsController(
    CreateDrawingUseCase create,
    GetDrawingUseCase get,
    ManageDrawingParticipantsUseCase participants,
    DrawNamesUseCase drawNames,
    AnswerDrawingUseCase answer,
    CancelDrawingUseCase cancel,
    GetMyDrawingInvitationsUseCase myInvitations,
    GetMyRecipientsUseCase myRecipients) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDrawingRequest request, CancellationToken ct) =>
        Ok(new { id = await create.ExecuteAsync(User.GetUserId(), request, ct) });

    [HttpGet("invitations/me")]
    public async Task<IActionResult> MyInvitations(CancellationToken ct) =>
        Ok(await myInvitations.ExecuteAsync(User.GetUserId(), ct));

    [HttpGet("recipients/me")]
    public async Task<IActionResult> MyRecipients(CancellationToken ct) =>
        Ok(await myRecipients.ExecuteAsync(User.GetUserId(), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        Ok(await get.ExecuteAsync(User.GetUserId(), id, ct));

    [HttpPost("{id:guid}/participants")]
    public async Task<IActionResult> AddParticipants(Guid id, [FromBody] AddDrawingParticipantsRequest request, CancellationToken ct) =>
        Ok(new { added = await participants.AddAsync(User.GetUserId(), id, request, ct) });

    [HttpDelete("{id:guid}/participants/{participantId:guid}")]
    public async Task<IActionResult> RemoveParticipant(Guid id, Guid participantId, CancellationToken ct)
    {
        await participants.RemoveAsync(User.GetUserId(), id, participantId, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/exclusions")]
    public async Task<IActionResult> SetExclusions(Guid id, [FromBody] SetDrawingExclusionsRequest request, CancellationToken ct)
    {
        await participants.SetExclusionsAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/draw")]
    public async Task<IActionResult> Draw(Guid id, [FromBody] DrawNamesRequest request, CancellationToken ct)
    {
        await drawNames.ExecuteAsync(User.GetUserId(), id, request, ct);
        return NoContent();
    }

    /// <summary>Joins yourself, or (as their buddy) someone else invited to the drawing.</summary>
    [HttpPost("{id:guid}/participants/{participantId:guid}/join")]
    public async Task<IActionResult> Join(Guid id, Guid participantId, CancellationToken ct)
    {
        await answer.JoinAsync(User.GetUserId(), id, participantId, ct);
        return NoContent();
    }

    /// <summary>Declines the invitation, or leaves after joining.</summary>
    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id, CancellationToken ct)
    {
        await answer.DeclineAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await cancel.CancelAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/undo-cancel")]
    public async Task<IActionResult> UndoCancel(Guid id, CancellationToken ct)
    {
        await cancel.UndoAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
