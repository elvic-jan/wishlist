using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

public class LoginUserUseCase(IUserRepository users, AuthSessionFactory sessions, ILoginAttemptLimiter limiter)
{
    public const string TooManyAttempts =
        "Too many wrong passwords. Try again in 15 minutes, sign in with Face ID or a fingerprint, or reset your password.";

    public async Task<(AuthResponse Response, string RefreshToken)> ExecuteAsync(LoginRequest request, string ipAddress, CancellationToken ct = default)
    {
        // Refused before the password is looked at, so a right guess is never confirmed.
        if (limiter.IsBlocked(request.Email, ipAddress))
            throw new TooManyRequestsException(TooManyAttempts);

        var user = await users.GetByEmailAsync(request.Email, ct);
        // Checked for unknown emails too, so timing doesn't reveal who has an account.
        var passwordOk = await users.CheckPasswordAsync(user?.Id ?? Guid.Empty, request.Password, ct);
        if (user is null || !passwordOk)
        {
            limiter.RecordFailure(request.Email, ipAddress);
            throw new DomainException("Invalid credentials.");
        }

        return await sessions.CreateAsync(user, SignInMethod.Password, ct: ct);
    }
}
