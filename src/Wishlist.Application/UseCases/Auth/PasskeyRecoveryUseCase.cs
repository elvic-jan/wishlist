using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

/// <summary>
/// "Can't sign in?" with a passkey: a valid recovery (password-reset) token lets
/// the holder add a passkey on the device they're on. Completing it uses up the
/// token, confirms the email (the link was delivered there) and signs out every
/// other session, like a password reset does.
/// </summary>
public class PasskeyRecoveryUseCase(
    IUserRepository users,
    IPasskeyService passkeys,
    ITokenService tokens,
    IUnitOfWork unitOfWork,
    AuthSessionFactory sessions,
    AccountEmails emails)
{
    public async Task<PasskeyOptions> BeginAsync(PasskeyRecoveryOptionsRequest request, CancellationToken ct = default)
    {
        var user = await RequireValidLinkAsync(request.Email, request.Token, ct);
        return await passkeys.BeginRegistrationAsync(new PasskeyUser(user.Id, user.Email, user.DisplayName), ct);
    }

    public async Task<(AuthResponse Response, string RefreshToken)> CompleteAsync(
        PasskeyRecoveryRequest request, string state, CancellationToken ct = default)
    {
        var user = await RequireValidLinkAsync(request.Email, request.Token, ct);

        var verified = await passkeys.CompleteRegistrationAsync(request.CredentialJson, state, ct);
        if (verified.User.Id != user.Id)
            throw new DomainException(PasskeyMessages.InvalidRecoveryLink);

        var name = PasskeyNames.Clean(request.PasskeyName);
        await unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            await passkeys.AddAsync(user.Id, verified.Credential, name, innerCt);
            await users.InvalidateSecurityTokensAsync(user.Id, innerCt);
            await users.MarkEmailConfirmedAsync(user.Id, innerCt);
            await tokens.RevokeAllUserRefreshTokensAsync(user.Id, innerCt);
        }, ct);

        await emails.SendPasskeyAddedAsync(user, name, ct);
        return await sessions.CreateAsync(user, ct: ct);
    }

    private async Task<User> RequireValidLinkAsync(string? email, string? token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            throw new DomainException(PasskeyMessages.InvalidRecoveryLink);

        var user = await users.GetByEmailAsync(email, ct);
        if (user is null || !await users.VerifyPasswordResetTokenAsync(user.Id, token, ct))
            throw new DomainException(PasskeyMessages.InvalidRecoveryLink);

        return user;
    }
}
