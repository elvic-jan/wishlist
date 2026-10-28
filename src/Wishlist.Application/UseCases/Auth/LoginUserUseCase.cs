using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

public class LoginUserUseCase(IUserRepository users, ITokenService tokens, ILoginAttemptLimiter limiter)
{
    public const string TooManyAttempts =
        "Too many wrong passwords. Try again in 15 minutes, sign in with Face ID or a fingerprint, or reset your password.";

    public async Task<(AuthResponse Response, string RefreshToken)> ExecuteAsync(LoginRequest request, string ipAddress, CancellationToken ct = default)
    {
        // Refused before the password is looked at, so a right guess is never confirmed.
        if (limiter.IsBlocked(request.Email, ipAddress))
            throw new TooManyRequestsException(TooManyAttempts);

        var user = await users.GetByEmailAsync(request.Email, ct);
        if (user is null || !await users.CheckPasswordAsync(user.Id, request.Password, ct))
        {
            limiter.RecordFailure(request.Email, ipAddress);
            throw new DomainException("Invalid credentials.");
        }

        var accessToken = tokens.GenerateAccessToken(user);
        var refreshToken = await tokens.CreateRefreshTokenAsync(user.Id, ct);

        return (new AuthResponse(accessToken, new UserDto(user.Id, user.Email, user.DisplayName)), refreshToken.Token);
    }
}
