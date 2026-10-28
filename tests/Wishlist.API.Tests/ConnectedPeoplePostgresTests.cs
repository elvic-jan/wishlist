using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Wishlists;
using Wishlist.Domain.Enums;

namespace Wishlist.API.Tests;

/// <summary>The "people I share a circle with" query, against real Postgres.</summary>
[Collection(PostgresCollection.Name)]
public class ConnectedPeoplePostgresTests
{
    [PostgresFact]
    public async Task Lists_each_circle_mate_once_with_only_the_shared_circles()
    {
        await using var api = new PostgresApi(Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!);
        await api.ResetDatabaseAsync();
        await using var scope = api.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var users = services.GetRequiredService<IUserRepository>();
        var me = await users.CreateAsync("me@example.test", "Me", "Passw0rd!long");
        var anna = await users.CreateAsync("anna@example.test", "anna", "Passw0rd!long");
        var bob = await users.CreateAsync("bob@example.test", "Bob", "Passw0rd!long");
        var colleague = await users.CreateAsync("colleague@example.test", "Colleague", "Passw0rd!long");
        var stranger = await users.CreateAsync("stranger@example.test", "Stranger", "Passw0rd!long");

        var groups = services.GetRequiredService<IGroupRepository>();
        var family = await groups.CreateAsync("Family", me.Id);
        var friends = await groups.CreateAsync("Friends", me.Id);
        var work = await groups.CreateAsync("Work", bob.Id);
        foreach (var u in new[] { me, anna, bob })
            await groups.AddMemberAsync(family.Id, u.Id, GroupMemberRole.Member);
        await groups.AddMemberAsync(friends.Id, me.Id, GroupMemberRole.Member);
        await groups.AddMemberAsync(friends.Id, bob.Id, GroupMemberRole.Member);
        await groups.AddMemberAsync(work.Id, bob.Id, GroupMemberRole.Member);
        await groups.AddMemberAsync(work.Id, colleague.Id, GroupMemberRole.Member);
        var alone = await groups.CreateAsync("Alone", stranger.Id);
        await groups.AddMemberAsync(alone.Id, stranger.Id, GroupMemberRole.Member);

        var people = await services.GetRequiredService<ListConnectedPeopleUseCase>().ExecuteAsync(me.Id);

        Assert.Equal(["anna", "Bob"], people.Select(p => p.DisplayName));
        Assert.Equal(["Family"], people[0].Circles.Select(c => c.Name));
        Assert.Equal(["Family", "Friends"], people[1].Circles.Select(c => c.Name));
        Assert.Empty(await services.GetRequiredService<ListConnectedPeopleUseCase>().ExecuteAsync(stranger.Id));
    }
}
