using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wishlist.Application.UseCases.Auth;

namespace Wishlist.API.Controllers;

/// <summary>The signed-in user's sign-in methods and sessions.</summary>
[ApiController]
[RequireHttps]
[Authorize]
[Route("api/auth")]
public class AccountSecurityController(
    PasskeyCeremonyCookie ceremonies,
    AccountSecurityUseCase security,
    EmailConfirmationUseCase emailConfirmation) : ControllerBase
{
    [HttpGet("security")]
    public async Task<IActionResult> Overview(CancellationToken ct) =>
        Ok(await security.GetOverviewAsync(UserId, ct));

    [HttpPost("passkeys/options")]
    public async Task<IActionResult> AddOptions(CancellationToken ct)
    {
        var options = await security.BeginAddPasskeyAsync(UserId, ct);
        ceremonies.Issue(Response, PasskeyCeremony.Add, options.State);
        return Ok(new { options = JsonDocument.Parse(options.OptionsJson).RootElement });
    }

    [HttpPost("passkeys")]
    public async Task<IActionResult> Add([FromBody] PasskeyAddBody body, CancellationToken ct)
    {
        var state = ceremonies.Take(Request, Response, PasskeyCeremony.Add);
        if (state is null) return BadRequest(new { error = "The passkey request expired. Please try again." });

        var passkey = await security.CompleteAddPasskeyAsync(UserId, body.CredentialJson, state, body.Name, ct);
        return StatusCode(StatusCodes.Status201Created, passkey);
    }

    [HttpPatch("passkeys/{id}")]
    public async Task<IActionResult> Rename(string id, [FromBody] RenamePasskeyBody body, CancellationToken ct)
    {
        await security.RenamePasskeyAsync(UserId, id, body.Name, ct);
        return NoContent();
    }

    [HttpDelete("passkeys/{id}")]
    public async Task<IActionResult> Remove(string id, CancellationToken ct)
    {
        await security.RemovePasskeyAsync(UserId, id, ct);
        return NoContent();
    }

    [HttpPost("confirm-email/resend")]
    public async Task<IActionResult> ResendConfirmation(CancellationToken ct)
    {
        await emailConfirmation.ResendAsync(UserId, ct);
        return Accepted();
    }

    [HttpPost("sessions/revoke-others")]
    public async Task<IActionResult> SignOutOtherDevices(CancellationToken ct)
    {
        await security.SignOutOtherDevicesAsync(UserId, RefreshTokenCookie.Read(Request), ct);
        return NoContent();
    }

    private Guid UserId => Guid.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new InvalidOperationException("User ID claim missing."));
}
