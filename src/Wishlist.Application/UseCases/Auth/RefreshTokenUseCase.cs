using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

public class RefreshTokenUseCase(IUserRepository users, ITokenService tokens)
{
    public async Task<(string AccessToken, string NewRefreshToken)> ExecuteAsync(string refreshToken, CancellationToken ct = default)
    {
        var existing = await tokens.GetActiveRefreshTokenAsync(refreshToken, ct)
            ?? throw new DomainException("Invalid or expired refresh token.");

        await tokens.RevokeRefreshTokenAsync(refreshToken, ct);

        var user = await users.GetByIdAsync(existing.UserId, ct)
            ?? throw new NotFoundException("User not found.");

        var newRefresh = await tokens.CreateRefreshTokenAsync(user.Id, ct);
        var accessToken = tokens.GenerateAccessToken(user);

        return (accessToken, newRefresh.Token);
    }
}
