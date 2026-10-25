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

    public async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(config["Jwt:RefreshTokenExpiryDays"]!)),
            CreatedAt = DateTime.UtcNow
        };
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);
        return token;
    }

    public async Task<RefreshToken?> GetActiveRefreshTokenAsync(string token, CancellationToken ct = default)
    {
        var rt = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token, ct);
        return rt?.IsActive == true ? rt : null;
    }

    public async Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default)
    {
        var rt = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == token, ct);
        if (rt is not null)
        {
            rt.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var t in tokens)
            t.RevokedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }

    public async Task RevokeOtherUserRefreshTokensAsync(Guid userId, string? keepToken, CancellationToken ct = default)
    {
        var tokens = await db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null && r.Token != keepToken)
            .ToListAsync(ct);

        foreach (var t in tokens)
            t.RevokedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
