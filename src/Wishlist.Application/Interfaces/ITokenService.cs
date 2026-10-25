using Wishlist.Domain.Entities;

namespace Wishlist.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, CancellationToken ct = default);
    Task<RefreshToken?> GetActiveRefreshTokenAsync(string token, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default);
    Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Revokes every active refresh token of the user except <paramref name="keepToken"/>.</summary>
    Task RevokeOtherUserRefreshTokensAsync(Guid userId, string? keepToken, CancellationToken ct = default);
}
