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
        Ok(await getMessages.ExecuteAsync(User.GetUserId(), ct));

    [HttpPost("{id}/dismiss")]
    public async Task<IActionResult> Dismiss(string id, CancellationToken ct)
    {
        await dismiss.ExecuteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
