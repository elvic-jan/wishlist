using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.TestData;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.API.Tests;

/// <summary>
/// Staging's test data and its reset, against real Postgres: seeding creates
/// people who share circles and a reservation, only once; after the deploy
/// empties the database (as .github/workflows/deploy-environment.yml does) the
/// schema, the app login and seeding all work again.
/// </summary>
[Collection(PostgresCollection.Name)]
public class TestDataPostgresTests
{
    private const string Inbox = "tester@example.test";
    private const string AppPassword = "test-app-login-password";

    private static string AdminConnection => Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!;

    private static string AppConnection => new NpgsqlConnectionStringBuilder(AdminConnection)
    {
        Username = "wishlist_app",
        Password = AppPassword,
    }.ConnectionString;

    private static string RepoFile(params string[] path)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Wishlist.sln"))) dir = dir.Parent;
        return File.ReadAllText(Path.Combine([dir!.FullName, .. path]));
    }

    [PostgresFact]
    public async Task Seeding_creates_shared_test_data_once()
    {
        await using var api = new PostgresApi(AdminConnection);
        await api.ResetDatabaseAsync();
        await using var scope = api.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var seed = services.GetRequiredService<SeedTestDataUseCase>();

        Assert.True(await seed.ExecuteAsync(Inbox));
        Assert.False(await seed.ExecuteAsync(Inbox));

        var users = services.GetRequiredService<IUserRepository>();
        var anna = await users.GetByEmailAsync("tester+anna@example.test");
        var ben = await users.GetByEmailAsync("tester+ben@example.test");
        Assert.NotNull(anna);
        Assert.NotNull(ben);
        Assert.False(anna.HasPassword);
        Assert.True(await services.GetRequiredService<IGroupRepository>().SharesGroupAsync(anna.Id, ben.Id));

        var db = services.GetRequiredService<WishlistDbContext>();
        Assert.Equal(SeedTestDataUseCase.People.Count, await db.Users.CountAsync());
        Assert.Equal(1, await db.GiftClaims.CountAsync());
    }

    [PostgresFact]
    public async Task After_the_deploy_empties_the_database_everything_works_again()
    {
        await using (var admin = new PostgresApi(AdminConnection))
        {
            await admin.ResetDatabaseAsync();
            await using var scope = admin.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<WishlistDbContext>();
            await scope.ServiceProvider.GetRequiredService<SeedTestDataUseCase>().ExecuteAsync(Inbox);

            // What the deploy does when the database has a migration the build doesn't.
            await db.Database.ExecuteSqlRawAsync("""
                DROP SCHEMA public CASCADE;
                CREATE SCHEMA public;
                GRANT USAGE ON SCHEMA public TO PUBLIC;
                """);
            await db.Database.MigrateAsync();
            await db.Database.ExecuteSqlRawAsync(RepoFile("infra", "sql", "app-role.sql"));
            await db.Database.ExecuteSqlRawAsync($"ALTER ROLE wishlist_app PASSWORD '{AppPassword}'");
            Assert.Equal(0, await db.Users.CountAsync());
        }

        // The app, as wishlist_app, seeds the empty database again.
        await using var app = new PostgresApi(AppConnection);
        await using var appScope = app.Services.CreateAsyncScope();
        Assert.True(await appScope.ServiceProvider.GetRequiredService<SeedTestDataUseCase>().ExecuteAsync(Inbox));
    }
}
