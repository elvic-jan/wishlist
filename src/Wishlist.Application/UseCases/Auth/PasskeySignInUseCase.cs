using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

public class PasskeySignInUseCase(IPasskeyService passkeys, IUserRepository users, AuthSessionFactory sessions)
{
    public Task<PasskeyOptions> BeginAsync(CancellationToken ct = default) => passkeys.BeginSignInAsync(ct);

    public async Task<(AuthResponse Response, string RefreshToken)> CompleteAsync(
        string credentialJson, string state, CancellationToken ct = default)
    {
        var userId = await passkeys.CompleteSignInAsync(credentialJson, state, ct);
        var user = await users.GetByIdAsync(userId, ct)
            ?? throw new DomainException(PasskeyMessages.SignInFailed);
        return await sessions.CreateAsync(user, ct: ct);
    }
}

public static class PasskeyMessages
{
    public const string SignInFailed = "Passkey sign-in failed.";
    public const string NotSaved = "The passkey couldn't be saved. Please try again.";
    public const string InvalidRecoveryLink = "This link is invalid or has expired. Request a new one from the sign-in page.";
}
