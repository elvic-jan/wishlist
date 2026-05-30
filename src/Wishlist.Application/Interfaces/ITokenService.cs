using Wishlist.Domain.Entities;

namespace Wishlist.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, CancellationToken ct = default);
    Task<RefreshToken?> GetActiveRefreshTokenAsync(string token, CancellationToken ct = default);
    Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default);
    Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken ct = default);
}
