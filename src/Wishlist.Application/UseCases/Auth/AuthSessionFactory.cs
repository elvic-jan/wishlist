using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;

namespace Wishlist.Application.UseCases.Auth;

/// <summary>Every way of signing in ends here: a short-lived access token plus a refresh token.</summary>
public class AuthSessionFactory(ITokenService tokens)
{
    public async Task<(AuthResponse Response, string RefreshToken)> CreateAsync(
        User user, Guid? joinedGroupId = null, CancellationToken ct = default)
    {
        var accessToken = tokens.GenerateAccessToken(user);
        var refreshToken = await tokens.CreateRefreshTokenAsync(user.Id, ct);
        return (new AuthResponse(accessToken, new UserDto(user.Id, user.Email, user.DisplayName), joinedGroupId),
            refreshToken.Token);
    }
}
