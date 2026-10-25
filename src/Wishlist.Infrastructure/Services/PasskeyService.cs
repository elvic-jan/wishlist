using System.Buffers.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Auth;
using Wishlist.Domain.Exceptions;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Services;

/// <summary>
/// WebAuthn on top of Identity's <see cref="IPasskeyHandler{TUser}"/>, which
/// checks challenge, origin, RP ID, user verification, signature, counter and
/// duplicate credentials. Failures are logged in detail and surfaced as a
/// generic <see cref="DomainException"/>.
/// </summary>
public sealed class PasskeyService(
    IPasskeyHandler<ApplicationUser> handler,
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    ILogger<PasskeyService> logger) : IPasskeyService
{
    private HttpContext Http => httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("Passkey ceremonies run within an HTTP request.");

    public async Task<PasskeyOptions> BeginRegistrationAsync(PasskeyUser user, CancellationToken ct = default)
    {
        var result = await handler.MakeCreationOptionsAsync(new PasskeyUserEntity
        {
            Id = user.Id.ToString(),
            Name = user.Name,
            DisplayName = user.DisplayName,
        }, Http);
        return new PasskeyOptions(result.CreationOptionsJson, result.AttestationState ?? string.Empty);
    }

    public async Task<VerifiedPasskey> CompleteRegistrationAsync(
        string credentialJson, string state, CancellationToken ct = default)
    {
        var result = await handler.PerformAttestationAsync(new PasskeyAttestationContext
        {
            HttpContext = Http,
            CredentialJson = credentialJson,
            AttestationState = state,
        });

        if (!result.Succeeded || !Guid.TryParse(result.UserEntity.Id, out var userId))
        {
            logger.LogWarning(result.Failure, "Passkey registration rejected: {Reason}", result.Failure?.Message);
            throw new DomainException(PasskeyMessages.NotSaved);
        }

        var p = result.Passkey;
        return new VerifiedPasskey(
            new PasskeyUser(userId, result.UserEntity.Name, result.UserEntity.DisplayName),
            new PasskeyCredential(
                p.CredentialId, p.PublicKey, p.CreatedAt, p.SignCount, p.Transports,
                p.IsUserVerified, p.IsBackupEligible, p.IsBackedUp, p.AttestationObject, p.ClientDataJson));
    }

    public async Task AddAsync(Guid userId, PasskeyCredential credential, string? name, CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId);
        var info = new UserPasskeyInfo(
            credential.CredentialId, credential.PublicKey, credential.CreatedAt, credential.SignCount,
            credential.Transports, credential.IsUserVerified, credential.IsBackupEligible, credential.IsBackedUp,
            credential.AttestationObject, credential.ClientDataJson)
        {
            Name = name,
        };
        ThrowIfFailed(await userManager.AddOrUpdatePasskeyAsync(user, info), "store a passkey");
    }

    public async Task<PasskeyOptions> BeginSignInAsync(CancellationToken ct = default)
    {
        // No user: the browser offers every passkey it holds for this site
        // (discoverable credentials), so nobody has to type an email first.
        var result = await handler.MakeRequestOptionsAsync(null, Http);
        return new PasskeyOptions(result.RequestOptionsJson, result.AssertionState ?? string.Empty);
    }

    public async Task<Guid> CompleteSignInAsync(string credentialJson, string state, CancellationToken ct = default)
    {
        var result = await handler.PerformAssertionAsync(new PasskeyAssertionContext
        {
            HttpContext = Http,
            CredentialJson = credentialJson,
            AssertionState = state,
        });

        if (!result.Succeeded)
        {
            logger.LogWarning(result.Failure, "Passkey sign-in rejected: {Reason}", result.Failure?.Message);
            throw new DomainException(PasskeyMessages.SignInFailed);
        }

        // Persist the new signature counter and backup state.
        ThrowIfFailed(await userManager.AddOrUpdatePasskeyAsync(result.User, result.Passkey), "update a passkey");
        return result.User.Id;
    }

    public async Task<IReadOnlyList<PasskeySummary>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId);
        var passkeys = await userManager.GetPasskeysAsync(user);
        return passkeys
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PasskeySummary(Base64Url.EncodeToString(p.CredentialId), p.Name, p.CreatedAt, p.IsBackedUp))
            .ToList();
    }

    public async Task<bool> RenameAsync(Guid userId, string passkeyId, string name, CancellationToken ct = default)
    {
        var (user, passkey) = await FindAsync(userId, passkeyId);
        if (passkey is null) return false;

        passkey.Name = name;
        ThrowIfFailed(await userManager.AddOrUpdatePasskeyAsync(user, passkey), "rename a passkey");
        return true;
    }

    public async Task<bool> RemoveAsync(Guid userId, string passkeyId, CancellationToken ct = default)
    {
        var (user, passkey) = await FindAsync(userId, passkeyId);
        if (passkey is null) return false;

        ThrowIfFailed(await userManager.RemovePasskeyAsync(user, passkey.CredentialId), "remove a passkey");
        return true;
    }

    private async Task<(ApplicationUser User, UserPasskeyInfo? Passkey)> FindAsync(Guid userId, string passkeyId)
    {
        var user = await RequireUserAsync(userId);
        byte[] credentialId;
        try
        {
            credentialId = Base64Url.DecodeFromChars(passkeyId);
        }
        catch (FormatException)
        {
            return (user, null);
        }

        return (user, await userManager.GetPasskeyAsync(user, credentialId));
    }

    private async Task<ApplicationUser> RequireUserAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

    private static void ThrowIfFailed(IdentityResult result, string action)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to {action}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }
}
