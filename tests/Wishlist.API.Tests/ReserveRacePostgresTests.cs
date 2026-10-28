using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.GiftClaims;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.API.Tests;

/// <summary>Two people reserving the same gift at the same moment: one gets it, the other a 409, never a 500.</summary>
[Collection(PostgresCollection.Name)]
public class ReserveRacePostgresTests
{
    [PostgresFact]
    public async Task Of_two_reservations_at_once_one_wins_and_the_other_gets_a_conflict()
    {
        await using var api = new PostgresApi(Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!);
        await api.ResetDatabaseAsync();

        Guid ownerId, itemId;
        var givers = new List<Guid>();
        await using (var setup = api.Services.CreateAsyncScope())
        {
            var services = setup.ServiceProvider;
            var users = services.GetRequiredService<IUserRepository>();
            var groups = services.GetRequiredService<IGroupRepository>();
            var owner = await users.CreateAsync("owner@example.test", "Owner", "Passw0rd!long");
            ownerId = owner.Id;
            var circle = await groups.CreateAsync("Family", owner.Id);
            await groups.AddMemberAsync(circle.Id, owner.Id, GroupMemberRole.Owner);
            for (var i = 0; i < 4; i++)
            {
                var giver = await users.CreateAsync($"giver{i}@example.test", $"Giver {i}", "Passw0rd!long");
                await groups.AddMemberAsync(circle.Id, giver.Id, GroupMemberRole.Member);
                givers.Add(giver.Id);
            }
            itemId = (await services.GetRequiredService<IWishlistRepository>().CreateAsync(owner.Id, "Bike", null, [])).Id;
        }

        var outcomes = await Task.WhenAll(givers.Select(async giverId =>
        {
            await using var scope = api.Services.CreateAsyncScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<ReserveGiftUseCase>().ExecuteAsync(giverId, ownerId, itemId);
                return "reserved";
            }
            catch (ConflictException)
            {
                return "conflict";
            }
        }));

        Assert.Single(outcomes, o => o == "reserved");
        Assert.All(outcomes.Where(o => o != "reserved"), o => Assert.Equal("conflict", o));
    }
}
