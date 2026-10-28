using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

public class RefreshTokenUseCase(IUserRepository users, ITokenService tokens)
{
    public async Task<(string AccessToken, string NewRefreshToken)> ExecuteAsync(string refreshToken, CancellationToken ct = default)
    {
        var userId = await tokens.ConsumeRefreshTokenAsync(refreshToken, ct)
            ?? throw new DomainException("Invalid or expired refresh token.");

        var user = await users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");

        var newRefresh = await tokens.CreateRefreshTokenAsync(user.Id, ct);
        return (tokens.GenerateAccessToken(user), newRefresh);
    }
}
