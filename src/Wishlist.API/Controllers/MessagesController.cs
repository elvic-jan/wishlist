using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wishlist.Application.Messages;

namespace Wishlist.API.Controllers;

/// <summary>Messages shown as lasting toasts (what's new, the old-site import offer, …).</summary>
[ApiController]
[Route("api/messages")]
[Authorize]
public class MessagesController(GetMyMessagesUseCase getMessages, DismissMessageUseCase dismiss) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken ct) =>
        Ok(await getMessages.ExecuteAsync(GetUserId(), ct));

    [HttpPost("{id}/dismiss")]
    public async Task<IActionResult> Dismiss(string id, CancellationToken ct)
    {
        await dismiss.ExecuteAsync(GetUserId(), id, ct);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim missing."));
}
