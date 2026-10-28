using Wishlist.Domain.Entities;

namespace Wishlist.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);

    /// <summary>Issues a new refresh token and returns it; only its hash is stored.</summary>
    Task<string> CreateRefreshTokenAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Revokes the token if it is still active and returns its user. Only one
    /// caller can consume a token, even when several present it at once.
    /// </summary>
    Task<Guid?> ConsumeRefreshTokenAsync(string token, CancellationToken ct = default);

    Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default);
    Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Revokes every active refresh token of the user except <paramref name="keepToken"/>.</summary>
    Task RevokeOtherUserRefreshTokensAsync(Guid userId, string? keepToken, CancellationToken ct = default);
}
