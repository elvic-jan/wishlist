using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.LegacyImport;
using Wishlist.Domain.Exceptions;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.API.Tests;

/// <summary>Loading the old site's export and importing from it, against real Postgres.</summary>
[Collection(PostgresCollection.Name)]
public class LegacyImportPostgresTests
{
    [PostgresFact]
    public async Task Load_import_and_message_state_work_against_postgres()
    {
        await using var api = new PostgresApi(Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!);
        await api.ResetDatabaseAsync();

        var users = TestXlsx.Write(["USER_ID", "NAME", "PASSWORD", "EMAILADDRESS"],
            [["1", "JOHN.DOE", "0123456789abcdef0123456789abcdef", "john@example.test"], ["2", "JANE.DOE", "", ""]]);
        var items = TestXlsx.Write(["ITEM_ID", "OWNER", "NAME", "DESCRIPTION", "STATE", "STATE_TS", "DELETED"],
            [["10", "1", "Bike", "see https://example.test/bike", "1", "45643.5", ""], ["11", "1", "Struck", "", "3", "", ""], ["20", "2", "Book", "", "2", "", ""]]);

        Guid ann, bob;
        await using (var scope = api.Services.CreateAsyncScope())
        {
            var s = scope.ServiceProvider;
            await s.GetRequiredService<LoadLegacyDataUseCase>().ExecuteAsync(users, items);
            ann = (await s.GetRequiredService<IUserRepository>().CreateAsync("ann@example.test", "Ann", "Passw0rd!long")).Id;
            bob = (await s.GetRequiredService<IUserRepository>().CreateAsync("bob@example.test", "Bob", "Passw0rd!long")).Id;
        }

        // Ann imports JOHN.DOE's bike.
        var status = await Run(api, s => s.GetRequiredService<ImportLegacyItemsUseCase>().ExecuteAsync(ann, new LegacyImportRequest("john.doe", [10, 20])));
        Assert.Equal(("JOHN.DOE", 1), (status.UserName, status.ItemCount));

        // Bob can't take JOHN.DOE; Ann can't import JANE.DOE as well.
        await Assert.ThrowsAsync<ConflictException>(() => Run(api, s => s.GetRequiredService<ImportLegacyItemsUseCase>().ExecuteAsync(bob, new LegacyImportRequest("JOHN.DOE", [10]))));
        await Assert.ThrowsAsync<ConflictException>(() => Run(api, s => s.GetRequiredService<ImportLegacyItemsUseCase>().ExecuteAsync(ann, new LegacyImportRequest("JANE.DOE", [20]))));

        // Even past the use case's checks, the database allows one claim per account.
        await Assert.ThrowsAsync<ConflictException>(() => Run(api, s => s.GetRequiredService<ILegacyRepository>().TryClaimAsync(2, ann, DateTime.UtcNow, 0)));

        // Loading again keeps the claim and adds nothing twice.
        await Run(api, s => s.GetRequiredService<LoadLegacyDataUseCase>().ExecuteAsync(users, items));

        await using (var scope = api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WishlistDbContext>();
            Assert.Equal(2, await db.LegacyUsers.CountAsync());
            Assert.Equal([10, 20], await db.LegacyItems.OrderBy(i => i.LegacyId).Select(i => i.LegacyId).ToListAsync());
            Assert.Equal(ann, (await db.LegacyUsers.SingleAsync(u => u.LegacyId == 1)).ClaimedByUserId);

            var bike = await db.WishlistItems.Include(i => i.Urls).SingleAsync(i => i.OwnerId == ann);
            Assert.Equal(("Bike", "see", "https://example.test/bike"), (bike.Title, bike.Description, bike.Urls.Single().Url));
        }

        // Dismissing a message twice stores it once.
        var states = new Func<IServiceProvider, IUserMessageStateRepository>(s => s.GetRequiredService<IUserMessageStateRepository>());
        await Run(api, s => states(s).DismissAsync(bob, "whats-new-2026-11", DateTime.UtcNow));
        await Run(api, s => states(s).DismissAsync(bob, "whats-new-2026-11", DateTime.UtcNow));
        Assert.Equal(["whats-new-2026-11"], await Run(api, s => states(s).GetDismissedAsync(bob)));
    }

    private static async Task<T> Run<T>(PostgresApi api, Func<IServiceProvider, Task<T>> work)
    {
        await using var scope = api.Services.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    private static async Task Run(PostgresApi api, Func<IServiceProvider, Task> work)
    {
        await using var scope = api.Services.CreateAsyncScope();
        await work(scope.ServiceProvider);
    }
}
