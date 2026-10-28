using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Enums;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.API.Tests;

/// <summary>
/// The app runs as wishlist_app (infra/sql/app-role.sql): everything it does
/// works with row rights only, and it can't change the schema.
/// </summary>
[Collection(PostgresCollection.Name)]
public class AppDatabaseLoginPostgresTests
{
    private const string AppPassword = "test-app-login-password";

    private static string AdminConnection => Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!;

    private static string AppConnection => new NpgsqlConnectionStringBuilder(AdminConnection)
    {
        Username = "wishlist_app",
        Password = AppPassword,
    }.ConnectionString;

    private static string RoleScript()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Wishlist.sln"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine(dir!.FullName, "infra", "sql", "app-role.sql"));
    }

    private static async Task PrepareDatabaseAsAdmin()
    {
        await using var admin = new PostgresApi(AdminConnection);
        await admin.ResetDatabaseAsync();
        await using var scope = admin.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WishlistDbContext>();
        // Twice: the deploy runs it every time.
        await db.Database.ExecuteSqlRawAsync(RoleScript());
        await db.Database.ExecuteSqlRawAsync(RoleScript());
        await db.Database.ExecuteSqlRawAsync($"ALTER ROLE wishlist_app PASSWORD '{AppPassword}'");
    }

    [PostgresFact]
    public async Task The_app_works_with_row_rights_only()
    {
        await PrepareDatabaseAsAdmin();

        await using var app = new PostgresApi(AppConnection);
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        // Startup already stored a Data Protection key; do what people do.
        var owner = await services.GetRequiredService<IUserRepository>().CreateAsync("owner@example.test", "Owner", "Passw0rd!long");
        var groups = services.GetRequiredService<IGroupRepository>();
        var circle = await groups.CreateAsync("Family", owner.Id);
        await groups.AddMemberAsync(circle.Id, owner.Id, GroupMemberRole.Owner);
        var item = await services.GetRequiredService<IWishlistRepository>()
            .CreateAsync(owner.Id, "Bike", "Red", ["https://shop.example/bike"]);
        Assert.NotEqual(Guid.Empty, item.Id);

        var tokens = services.GetRequiredService<ITokenService>();
        var token = await tokens.CreateRefreshTokenAsync(owner.Id);
        Assert.Equal(owner.Id, await tokens.ConsumeRefreshTokenAsync(token));
        await tokens.CreateRefreshTokenAsync(owner.Id); // cleanup deletes rows
        await tokens.RevokeAllUserRefreshTokensAsync(owner.Id);
    }

    [PostgresFact]
    public async Task The_app_login_cannot_change_the_schema()
    {
        await PrepareDatabaseAsAdmin();

        await using var connection = new NpgsqlConnection(AppConnection);
        await connection.OpenAsync();

        foreach (var sql in new[]
                 {
                     """DROP TABLE "WishlistItems";""",
                     """ALTER TABLE "WishlistItems" ADD COLUMN "Oops" text;""",
                     """CREATE TABLE "Sneaky" ("Id" int);""",
                     """DELETE FROM "__EFMigrationsHistory";""",
                 })
        {
            await using var command = new NpgsqlCommand(sql, connection);
            var error = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
        }
    }
}
