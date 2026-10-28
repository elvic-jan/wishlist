using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Infrastructure.Persistence;
using Wishlist.Infrastructure.Services;

namespace Wishlist.API.Tests;

/// <summary>Refresh tokens are stored as hashes, consumed once, and cleaned up, against real Postgres.</summary>
[Collection(PostgresCollection.Name)]
public class RefreshTokenPostgresTests
{
    private const string MigrationBeforeHashing = "20261027215909_AddCircleInvitations";

    private static PostgresApi Api() => new(Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!);

    private static async Task<User> NewUser(IServiceProvider services, string email) =>
        await services.GetRequiredService<IUserRepository>().CreateAsync(email, "Someone", "Passw0rd!long");

    [PostgresFact]
    public async Task Tokens_from_before_the_upgrade_keep_working()
    {
        await using var api = Api();
        await using var scope = api.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<WishlistDbContext>();

        await db.Database.EnsureDeletedAsync();
        await db.GetService<IMigrator>().MigrateAsync(MigrationBeforeHashing);
        var user = await NewUser(services, "old@example.test");
        const string plain = "an-old-plain-refresh-token";
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO "RefreshTokens" ("Id", "UserId", "Token", "ExpiresAt", "CreatedAt")
            VALUES ({Guid.NewGuid()}, {user.Id}, {plain}, {DateTime.UtcNow.AddDays(10)}, {DateTime.UtcNow})
            """);

        await db.Database.MigrateAsync();
        db.ChangeTracker.Clear();

        var stored = await db.RefreshTokens.AsNoTracking().SingleAsync();
        Assert.Equal(TokenService.Hash(plain), stored.TokenHash);
        Assert.Equal(user.Id, await services.GetRequiredService<ITokenService>().ConsumeRefreshTokenAsync(plain));
    }

    [PostgresFact]
    public async Task Only_the_hash_is_stored_and_it_is_no_use_as_a_token()
    {
        await using var api = Api();
        await api.ResetDatabaseAsync();
        await using var scope = api.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var tokens = services.GetRequiredService<ITokenService>();
        var user = await NewUser(services, "hash@example.test");

        var token = await tokens.CreateRefreshTokenAsync(user.Id);

        var stored = await services.GetRequiredService<WishlistDbContext>().RefreshTokens.AsNoTracking().SingleAsync();
        Assert.NotEqual(token, stored.TokenHash);
        Assert.Equal(TokenService.Hash(token), stored.TokenHash);
        Assert.Null(await tokens.ConsumeRefreshTokenAsync(stored.TokenHash));
        Assert.Equal(user.Id, await tokens.ConsumeRefreshTokenAsync(token));
        Assert.Null(await tokens.ConsumeRefreshTokenAsync(token)); // single use
    }

    [PostgresFact]
    public async Task Of_two_refreshes_at_once_exactly_one_wins()
    {
        await using var api = Api();
        await api.ResetDatabaseAsync();
        string token;
        await using (var setup = api.Services.CreateAsyncScope())
        {
            var user = await NewUser(setup.ServiceProvider, "race@example.test");
            token = await setup.ServiceProvider.GetRequiredService<ITokenService>().CreateRefreshTokenAsync(user.Id);
        }

        for (var round = 0; round < 5; round++)
        {
            var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
            {
                await using var scope = api.Services.CreateAsyncScope();
                return await scope.ServiceProvider.GetRequiredService<ITokenService>().ConsumeRefreshTokenAsync(token);
            }));

            Assert.Single(results, r => r is not null);

            // Next round races over a fresh token.
            await using var next = api.Services.CreateAsyncScope();
            token = await next.ServiceProvider.GetRequiredService<ITokenService>().CreateRefreshTokenAsync(results.First(r => r is not null)!.Value);
        }
    }

    [PostgresFact]
    public async Task New_tokens_clean_up_expired_and_long_revoked_ones()
    {
        await using var api = Api();
        await api.ResetDatabaseAsync();
        await using var scope = api.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<WishlistDbContext>();
        var user = await NewUser(services, "clean@example.test");
        var other = await NewUser(services, "other@example.test");
        var now = DateTime.UtcNow;

        RefreshToken Row(Guid userId, string name, DateTime expires, DateTime? revoked) => new()
        {
            Id = Guid.NewGuid(), UserId = userId, TokenHash = TokenService.Hash(name),
            CreatedAt = now.AddDays(-20), ExpiresAt = expires, RevokedAt = revoked,
        };
        db.RefreshTokens.AddRange(
            Row(user.Id, "expired", now.AddDays(-1), null),
            Row(user.Id, "revoked-long-ago", now.AddDays(5), now.AddDays(-2)),
            Row(user.Id, "revoked-just-now", now.AddDays(5), now.AddMinutes(-5)),
            Row(user.Id, "active", now.AddDays(5), null),
            Row(other.Id, "someone-elses-expired", now.AddDays(-1), null));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var fresh = await services.GetRequiredService<ITokenService>().CreateRefreshTokenAsync(user.Id);

        var left = await db.RefreshTokens.AsNoTracking().Select(r => r.TokenHash).ToListAsync();
        Assert.Equal(
            new[] { "revoked-just-now", "active", "someone-elses-expired" }.Select(TokenService.Hash).Append(TokenService.Hash(fresh)).Order(),
            left.Order());
    }
}
