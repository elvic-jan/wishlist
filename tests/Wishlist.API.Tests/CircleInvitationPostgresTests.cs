using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Groups;
using Wishlist.Domain.Enums;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.API.Tests;

/// <summary>The candidate query, the one-invitation-per-person rule and joining, against real Postgres.</summary>
[Collection(PostgresCollection.Name)]
public class CircleInvitationPostgresTests
{
    [PostgresFact]
    public async Task Invite_from_circles_end_to_end()
    {
        await using var api = new PostgresApi(Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!);
        await api.ResetDatabaseAsync();
        await using var scope = api.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var users = services.GetRequiredService<IUserRepository>();
        var me = await users.CreateAsync("me@example.test", "Me", "Passw0rd!long");
        var ann = await users.CreateAsync("ann@example.test", "Ann", "Passw0rd!long");
        var bob = await users.CreateAsync("bob@example.test", "Bob", "Passw0rd!long");
        var cleo = await users.CreateAsync("cleo@example.test", "Cleo", "Passw0rd!long");

        var groups = services.GetRequiredService<IGroupRepository>();
        var target = await groups.CreateAsync("Target", me.Id);
        var family = await groups.CreateAsync("Family", me.Id);
        var friends = await groups.CreateAsync("Friends", me.Id);
        foreach (var (g, u) in new[] { (target, me), (target, ann), (family, me), (family, ann), (family, bob), (family, cleo), (friends, me), (friends, bob) })
            await groups.AddMemberAsync(g.Id, u.Id, GroupMemberRole.Member);

        var candidates = services.GetRequiredService<GetInviteCandidatesUseCase>();
        var invite = services.GetRequiredService<InviteFromCirclesUseCase>();

        var before = await candidates.ExecuteAsync(me.Id, target.Id);
        Assert.Equal(["Family", "Friends"], before.Select(c => c.Name));
        Assert.Equal(["Bob", "Cleo"], before[0].Members.Select(m => m.DisplayName));
        Assert.Equal(["Bob"], before[1].Members.Select(m => m.DisplayName));

        Assert.Equal(new InviteFromCirclesResult(2, 1), await invite.ExecuteAsync(me.Id, target.Id, new InviteFromCirclesRequest([bob.Id, cleo.Id, ann.Id])));
        Assert.Equal(new InviteFromCirclesResult(0, 1), await invite.ExecuteAsync(me.Id, target.Id, new InviteFromCirclesRequest([bob.Id])));
        Assert.Single(api.Emails, m => m.To == bob.Email);
        Assert.All((await candidates.ExecuteAsync(me.Id, target.Id)).SelectMany(c => c.Members), m => Assert.True(m.Invited));

        var mine = services.GetRequiredService<GetMyCircleInvitationsUseCase>();
        var answer = services.GetRequiredService<AnswerCircleInvitationUseCase>();
        var bobsInvitation = Assert.Single(await mine.ExecuteAsync(bob.Id));
        Assert.Equal(("Target", "Me"), (bobsInvitation.CircleName, bobsInvitation.InvitedByName));

        await answer.JoinAsync(bob.Id, bobsInvitation.Id);
        Assert.True(await groups.IsMemberAsync(target.Id, bob.Id));
        Assert.Empty(await mine.ExecuteAsync(bob.Id));

        // Deleting a circle takes its open invitations with it.
        var db = services.GetRequiredService<WishlistDbContext>();
        await db.Groups.Where(g => g.Id == target.Id).ExecuteDeleteAsync();
        Assert.Empty(await mine.ExecuteAsync(cleo.Id));
    }
}
