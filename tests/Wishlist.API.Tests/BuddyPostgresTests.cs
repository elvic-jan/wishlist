using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Buddies;
using Wishlist.Application.UseCases.Wishlists;
using Wishlist.Domain.Enums;

namespace Wishlist.API.Tests;

/// <summary>The buddy queries (shared-circle filter, candidates) and "added by" names, against real Postgres.</summary>
[Collection(PostgresCollection.Name)]
public class BuddyPostgresTests
{
    [PostgresFact]
    public async Task Buddy_queries_follow_shared_circles()
    {
        await using var api = new PostgresApi(Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!);
        await api.ResetDatabaseAsync();
        await using var scope = api.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var users = services.GetRequiredService<IUserRepository>();
        var owner = await users.CreateAsync("owner@example.test", "Owner", "Passw0rd!long");
        var buddy = await users.CreateAsync("buddy@example.test", "Buddy", "Passw0rd!long");
        var giver = await users.CreateAsync("giver@example.test", "Giver", "Passw0rd!long");
        var stranger = await users.CreateAsync("stranger@example.test", "Stranger", "Passw0rd!long");

        var groups = services.GetRequiredService<IGroupRepository>();
        var family = await groups.CreateAsync("Family", owner.Id);
        var friends = await groups.CreateAsync("Friends", owner.Id);
        foreach (var u in new[] { owner, buddy, giver })
            await groups.AddMemberAsync(family.Id, u.Id, GroupMemberRole.Member);
        await groups.AddMemberAsync(friends.Id, owner.Id, GroupMemberRole.Member);
        await groups.AddMemberAsync(friends.Id, buddy.Id, GroupMemberRole.Member);

        var manage = services.GetRequiredService<ManageBuddiesUseCase>();
        var repo = services.GetRequiredService<IWishlistBuddyRepository>();

        // Candidates: each circle-mate once, minus self.
        Assert.Equal(["Buddy", "Giver"], (await manage.GetCandidatesAsync(owner.Id)).Select(c => c.DisplayName));

        await manage.AddAsync(owner.Id, buddy.Id);
        await manage.AddAsync(owner.Id, buddy.Id);
        await Assert.ThrowsAsync<Domain.Exceptions.ForbiddenException>(() => manage.AddAsync(owner.Id, stranger.Id));
        Assert.Single(api.Emails, m => m.To == buddy.Email);
        Assert.Equal(["Giver"], (await manage.GetCandidatesAsync(owner.Id)).Select(c => c.DisplayName));
        Assert.Equal(["Buddy"], (await manage.GetBuddiesAsync(owner.Id)).Select(c => c.DisplayName));
        Assert.Equal(["Owner"], (await manage.GetBuddyOfAsync(buddy.Id)).Select(c => c.DisplayName));

        // The buddy adds a wish; both lists name them.
        var add = services.GetRequiredService<AddWishlistItemUseCase>();
        var added = await add.ExecuteAsBuddyAsync(buddy.Id, owner.Id, new AddWishlistItemRequest("Lego", null, null));
        Assert.Equal("Buddy", added.AddedBy?.DisplayName);
        var own = await services.GetRequiredService<GetOwnWishlistUseCase>().ExecuteAsync(owner.Id);
        Assert.Equal("Buddy", Assert.Single(own).AddedBy?.DisplayName);

        // Leaving one shared circle keeps the rights; leaving both drops them.
        await groups.RemoveMemberAsync(family.Id, buddy.Id);
        Assert.True(await repo.IsActiveBuddyAsync(owner.Id, buddy.Id));
        await groups.RemoveMemberAsync(friends.Id, buddy.Id);
        Assert.False(await repo.IsActiveBuddyAsync(owner.Id, buddy.Id));
        Assert.Empty(await manage.GetBuddiesAsync(owner.Id));
        Assert.Empty(await manage.GetBuddyOfAsync(buddy.Id));

        // Removing the buddy keeps their wish and its label.
        await manage.RemoveAsync(owner.Id, buddy.Id);
        own = await services.GetRequiredService<GetOwnWishlistUseCase>().ExecuteAsync(owner.Id);
        Assert.Equal("Buddy", Assert.Single(own).AddedBy?.DisplayName);
    }
}
