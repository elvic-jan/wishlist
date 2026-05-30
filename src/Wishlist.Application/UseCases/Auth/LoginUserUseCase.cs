using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

public class LoginUserUseCase(IUserRepository users, ITokenService tokens)
{
    public async Task<(AuthResponse Response, string RefreshToken)> ExecuteAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByEmailAsync(request.Email, ct)
            ?? throw new DomainException("Invalid credentials.");

        if (!await users.CheckPasswordAsync(user.Id, request.Password, ct))
            throw new DomainException("Invalid credentials.");

        var accessToken = tokens.GenerateAccessToken(user);
        var refreshToken = await tokens.CreateRefreshTokenAsync(user.Id, ct);

        return (new AuthResponse(accessToken, new UserDto(user.Id, user.Email, user.DisplayName)), refreshToken.Token);
    }
}
