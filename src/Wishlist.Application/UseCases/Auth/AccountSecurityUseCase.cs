using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

/// <summary>A signed-in user's ways of signing in: passkeys, password, sessions.</summary>
public class AccountSecurityUseCase(
    IUserRepository users,
    IPasskeyService passkeys,
    ITokenService tokens,
    AccountEmails emails)
{
    public async Task<SecurityOverviewDto> GetOverviewAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId, ct);
        var list = await passkeys.ListAsync(userId, ct);
        return new SecurityOverviewDto(user.EmailConfirmed, user.HasPassword, list);
    }

    public async Task<PasskeyOptions> BeginAddPasskeyAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId, ct);
        return await passkeys.BeginRegistrationAsync(new PasskeyUser(user.Id, user.Email, user.DisplayName), ct);
    }

    public async Task<PasskeySummary> CompleteAddPasskeyAsync(
        Guid userId, string credentialJson, string state, string? name, CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId, ct);
        var verified = await passkeys.CompleteRegistrationAsync(credentialJson, state, ct);
        if (verified.User.Id != userId)
            throw new DomainException(PasskeyMessages.NotSaved);

        var cleanName = PasskeyNames.Clean(name);
        await passkeys.AddAsync(userId, verified.Credential, cleanName, ct);
        await emails.SendPasskeyAddedAsync(user, cleanName, ct);

        var id = Base64UrlId(verified.Credential.CredentialId);
        return (await passkeys.ListAsync(userId, ct)).First(p => p.Id == id);
    }

    public async Task RenamePasskeyAsync(Guid userId, string passkeyId, string? name, CancellationToken ct = default)
    {
        var cleanName = PasskeyNames.Clean(name)
            ?? throw new DomainException("Give the passkey a name.");
        if (!await passkeys.RenameAsync(userId, passkeyId, cleanName, ct))
            throw new NotFoundException("Passkey not found.");
    }

    public async Task RemovePasskeyAsync(Guid userId, string passkeyId, CancellationToken ct = default)
    {
        var user = await RequireUserAsync(userId, ct);
        var list = await passkeys.ListAsync(userId, ct);
        if (list.All(p => p.Id != passkeyId))
            throw new NotFoundException("Passkey not found.");

        if (!user.HasPassword && list.Count <= 1)
            throw new ConflictException(
                "This is your only way to sign in. Add another passkey first, then remove this one.");

        await passkeys.RemoveAsync(userId, passkeyId, ct);
    }

    public Task SignOutOtherDevicesAsync(Guid userId, string? currentRefreshToken, CancellationToken ct = default) =>
        tokens.RevokeOtherUserRefreshTokensAsync(userId, currentRefreshToken, ct);

    private async Task<User> RequireUserAsync(Guid userId, CancellationToken ct) =>
        await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User not found.");

    private static string Base64UrlId(byte[] id) =>
        Convert.ToBase64String(id).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
