namespace Wishlist.Application.Interfaces;

/// <summary>
/// WebAuthn ceremonies and passkey storage. Options and credentials travel as
/// the browser's JSON; <see cref="PasskeyOptions.State"/> is opaque and must be
/// handed back unchanged (the API keeps it in a protected cookie).
/// Verification failures throw <see cref="Wishlist.Domain.Exceptions.DomainException"/>
/// with a generic message.
/// </summary>
public interface IPasskeyService
{
    /// <summary>Options for creating a passkey bound to <paramref name="user"/> (new or existing account).</summary>
    Task<PasskeyOptions> BeginRegistrationAsync(PasskeyUser user, CancellationToken ct = default);

    /// <summary>Verifies a new credential. Nothing is stored; call <see cref="AddAsync"/> once the account exists.</summary>
    Task<VerifiedPasskey> CompleteRegistrationAsync(string credentialJson, string state, CancellationToken ct = default);

    Task AddAsync(Guid userId, PasskeyCredential credential, string? name, CancellationToken ct = default);

    /// <summary>Options for signing in with any discoverable passkey.</summary>
    Task<PasskeyOptions> BeginSignInAsync(CancellationToken ct = default);

    /// <summary>Verifies an assertion, stores the new signature counter, and returns the account it belongs to.</summary>
    Task<Guid> CompleteSignInAsync(string credentialJson, string state, CancellationToken ct = default);

    Task<IReadOnlyList<PasskeySummary>> ListAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns <c>false</c> if the user has no passkey with that ID.</summary>
    Task<bool> RenameAsync(Guid userId, string passkeyId, string name, CancellationToken ct = default);

    /// <summary>Returns <c>false</c> if the user has no passkey with that ID.</summary>
    Task<bool> RemoveAsync(Guid userId, string passkeyId, CancellationToken ct = default);
}

public sealed record PasskeyOptions(string OptionsJson, string State);

/// <summary>The account a passkey is created for: its ID becomes the WebAuthn user handle.</summary>
public sealed record PasskeyUser(Guid Id, string Name, string DisplayName);

public sealed record VerifiedPasskey(PasskeyUser User, PasskeyCredential Credential);

/// <summary>A verified, not yet stored credential.</summary>
public sealed record PasskeyCredential(
    byte[] CredentialId,
    byte[] PublicKey,
    DateTimeOffset CreatedAt,
    uint SignCount,
    string[]? Transports,
    bool IsUserVerified,
    bool IsBackupEligible,
    bool IsBackedUp,
    byte[] AttestationObject,
    byte[] ClientDataJson);

/// <param name="Id">The credential ID, base64url-encoded.</param>
/// <param name="IsBackedUp">Synced by a passkey provider (iCloud Keychain, Google Password Manager, …).</param>
public sealed record PasskeySummary(string Id, string? Name, DateTimeOffset CreatedAt, bool IsBackedUp);
