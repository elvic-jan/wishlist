using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Auth;

namespace Wishlist.API.Controllers;

/// <summary>
/// Passkey ceremonies for people who aren't signed in: sign-in, sign-up through
/// an invitation, and recovery through an emailed link. Each is an options call
/// (returns WebAuthn options for the browser, sets the ceremony cookie) followed
/// by a completion call carrying the browser's credential.
/// </summary>
[ApiController]
[RequireHttps]
[AllowAnonymous]
[Route("api/auth")]
public class PasskeyAuthController(
    PasskeyCeremonyCookie ceremonies,
    InviteCodeGuard inviteCodeGuard,
    PasskeySignInUseCase signIn,
    PasskeySignUpUseCase signUp,
    PasskeyRecoveryUseCase recovery,
    EmailConfirmationUseCase emailConfirmation) : ControllerBase
{
    private const string CeremonyExpired = "The passkey request expired. Please try again.";

    [HttpPost("passkey/login/options")]
    public async Task<IActionResult> SignInOptions(CancellationToken ct) =>
        Options(PasskeyCeremony.SignIn, await signIn.BeginAsync(ct));

    [HttpPost("passkey/login")]
    public async Task<IActionResult> SignIn([FromBody] PasskeyCredentialBody body, CancellationToken ct)
    {
        var state = ceremonies.Take(Request, Response, PasskeyCeremony.SignIn);
        if (state is null) return BadRequest(new { error = CeremonyExpired });

        var (response, refreshToken) = await signIn.CompleteAsync(body.CredentialJson, state, ct);
        RefreshTokenCookie.Set(Response, refreshToken);
        return Ok(response);
    }

    [HttpPost("passkey/register/options")]
    public Task<IActionResult> SignUpOptions([FromBody] PasskeySignUpOptionsRequest request, CancellationToken ct)
    {
        async Task<IActionResult> Attempt() => Options(PasskeyCeremony.SignUp, await signUp.BeginAsync(request, ct));

        return string.IsNullOrWhiteSpace(request.InviteCode)
            ? Attempt()
            : inviteCodeGuard.RunAsync(HttpContext, Attempt);
    }

    [HttpPost("passkey/register")]
    public Task<IActionResult> SignUp([FromBody] PasskeySignUpBody body, CancellationToken ct)
    {
        async Task<IActionResult> Attempt()
        {
            var state = ceremonies.Take(Request, Response, PasskeyCeremony.SignUp);
            if (state is null) return BadRequest(new { error = CeremonyExpired });

            var request = new PasskeySignUpRequest(
                body.Email, body.DisplayName, body.InviteCode, body.CredentialJson, body.PasskeyName);
            var (response, refreshToken) = await signUp.CompleteAsync(request, state, ct);
            RefreshTokenCookie.Set(Response, refreshToken);
            return Ok(response);
        }

        return string.IsNullOrWhiteSpace(body.InviteCode)
            ? Attempt()
            : inviteCodeGuard.RunAsync(HttpContext, Attempt);
    }

    [HttpPost("recovery/passkey/options")]
    public async Task<IActionResult> RecoveryOptions([FromBody] PasskeyRecoveryOptionsRequest request, CancellationToken ct) =>
        Options(PasskeyCeremony.Recovery, await recovery.BeginAsync(request, ct));

    [HttpPost("recovery/passkey")]
    public async Task<IActionResult> Recover([FromBody] PasskeyRecoveryBody body, CancellationToken ct)
    {
        var state = ceremonies.Take(Request, Response, PasskeyCeremony.Recovery);
        if (state is null) return BadRequest(new { error = CeremonyExpired });

        var request = new PasskeyRecoveryRequest(body.Email, body.Token, body.CredentialJson, body.PasskeyName);
        var (response, refreshToken) = await recovery.CompleteAsync(request, state, ct);
        RefreshTokenCookie.Set(Response, refreshToken);
        return Ok(response);
    }

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct) =>
        await emailConfirmation.ConfirmAsync(request, ct)
            ? NoContent()
            : BadRequest(new { error = "This confirmation link is invalid or has expired." });

    private OkObjectResult Options(PasskeyCeremony ceremony, PasskeyOptions options)
    {
        ceremonies.Issue(Response, ceremony, options.State);
        // The handler's options JSON is passed through as a JSON object, not a string.
        return Ok(new { options = JsonDocument.Parse(options.OptionsJson).RootElement });
    }
}

/// <summary>A browser credential (PublicKeyCredential.toJSON()) as sent by the SPA.</summary>
public record PasskeyCredentialBody(JsonElement Credential)
{
    public string CredentialJson => Credential.ValueKind == JsonValueKind.Object
        ? Credential.GetRawText()
        : throw new Wishlist.Domain.Exceptions.DomainException("A passkey credential is required.");
}

public record PasskeySignUpBody(
    string Email, string DisplayName, string? InviteCode, JsonElement Credential, string? PasskeyName)
    : PasskeyCredentialBody(Credential);

public record PasskeyRecoveryBody(string Email, string Token, JsonElement Credential, string? PasskeyName)
    : PasskeyCredentialBody(Credential);

public record PasskeyAddBody(JsonElement Credential, string? Name) : PasskeyCredentialBody(Credential);

public record RenamePasskeyBody(string? Name);
