using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Services;

public class TokenService(IConfiguration config, WishlistDbContext db) : ITokenService
{
    /// <summary>How long a revoked token is kept before cleanup removes it.</summary>
    private static readonly TimeSpan KeepRevokedFor = TimeSpan.FromDays(1);

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("displayName", user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiry = int.Parse(config["Jwt:AccessTokenExpiryMinutes"]!);
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// SHA-256 as lowercase hex. The token is 512 random bits, so a fast hash is
    /// enough: there is nothing to guess. Matches the migration's SQL.
    /// </summary>
    public static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public async Task<string> CreateRefreshTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var revokedBefore = now - KeepRevokedFor;
        await db.RefreshTokens
            .Where(r => r.UserId == userId && (r.ExpiresAt < now || r.RevokedAt < revokedBefore))
            .ExecuteDeleteAsync(ct);

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = Hash(token),
            ExpiresAt = now.AddDays(int.Parse(config["Jwt:RefreshTokenExpiryDays"]!)),
            CreatedAt = now
        });
        await db.SaveChangesAsync(ct);
        return token;
    }

    public async Task<Guid?> ConsumeRefreshTokenAsync(string token, CancellationToken ct = default)
    {
        var hash = Hash(token);
        var now = DateTime.UtcNow;

        // One conditional update: of several requests with the same token, only
        // the one that flips RevokedAt gets a row back.
        var revoked = await db.RefreshTokens
            .Where(r => r.TokenHash == hash && r.RevokedAt == null && r.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, now), ct);
        if (revoked == 0) return null;

        return await db.RefreshTokens
            .Where(r => r.TokenHash == hash)
            .Select(r => (Guid?)r.UserId)
            .FirstOrDefaultAsync(ct);
    }

    public async Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default)
    {
        var hash = Hash(token);
        await db.RefreshTokens
            .Where(r => r.TokenHash == hash && r.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, DateTime.UtcNow), ct);
    }

    public async Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken ct = default)
    {
        await db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, DateTime.UtcNow), ct);
    }

    public async Task RevokeOtherUserRefreshTokensAsync(Guid userId, string? keepToken, CancellationToken ct = default)
    {
        var keepHash = keepToken is null ? null : Hash(keepToken);
        await db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null && r.TokenHash != keepHash)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, DateTime.UtcNow), ct);
    }
}
