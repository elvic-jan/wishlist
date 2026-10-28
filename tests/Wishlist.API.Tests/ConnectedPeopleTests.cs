using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Wishlists;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;

namespace Wishlist.API.Tests;

/// <summary>The Wishlists page's list: everyone the user shares a circle with, each once.</summary>
public class ConnectedPeopleTests
{
    private readonly InMemoryStore _store = new();

    private User Person(string name)
    {
        var user = _store.AddUser($"{Guid.NewGuid()}@example.test");
        user.DisplayName = name;
        return user;
    }

    private Group Circle(string name, params User[] members)
    {
        var group = _store.AddGroup(name);
        foreach (var m in members)
            _store.Members[(group.Id, m.Id)] = new GroupMember { GroupId = group.Id, UserId = m.Id, Role = GroupMemberRole.Member };
        return group;
    }

    private Task<IReadOnlyList<ConnectedPersonDto>> PeopleOf(User user) =>
        new ListConnectedPeopleUseCase(new InMemoryGroups(_store)).ExecuteAsync(user.Id);

    [Fact]
    public async Task Someone_in_several_shared_circles_appears_once_with_each_circle()
    {
        var me = Person("Me");
        var anna = Person("Anna");
        Circle("Friends", me, anna);
        Circle("Family", me, anna);

        var person = Assert.Single(await PeopleOf(me));

        Assert.Equal(anna.Id, person.UserId);
        Assert.Equal("Anna", person.DisplayName);
        Assert.Equal(["Family", "Friends"], person.Circles.Select(c => c.Name));
    }

    [Fact]
    public async Task Only_shared_circles_are_listed_and_strangers_and_self_are_left_out()
    {
        var me = Person("Me");
        var anna = Person("Anna");
        var colleague = Person("Colleague");
        Circle("Family", me, anna);
        Circle("Work", anna, colleague);

        var people = await PeopleOf(me);

        var person = Assert.Single(people);
        Assert.Equal(anna.Id, person.UserId);
        Assert.Equal(["Family"], person.Circles.Select(c => c.Name));
    }

    [Fact]
    public async Task People_are_sorted_by_name_ignoring_case()
    {
        var me = Person("Me");
        Circle("Family", me, Person("bob"), Person("Carl"), Person("Anna"));

        Assert.Equal(["Anna", "bob", "Carl"], (await PeopleOf(me)).Select(p => p.DisplayName));
    }

    [Fact]
    public async Task Nobody_when_alone_or_in_no_circle()
    {
        var me = Person("Me");
        Assert.Empty(await PeopleOf(me));

        Circle("Just me", me);
        Assert.Empty(await PeopleOf(me));
    }

    [Fact]
    public async Task Endpoint_needs_a_session_and_returns_the_list()
    {
        await using var api = new InMemoryApi();
        var client = api.CreateClient();
        var me = api.Store.AddUser("me@example.test");
        var anna = api.Store.AddUser("anna@example.test");
        anna.DisplayName = "Anna";
        var family = api.Store.AddGroup("Family");
        api.Store.Members[(family.Id, me.Id)] = new GroupMember { GroupId = family.Id, UserId = me.Id };
        api.Store.Members[(family.Id, anna.Id)] = new GroupMember { GroupId = family.Id, UserId = anna.Id };

        var anonymous = await InMemoryApi.Send(client, HttpMethod.Get, "/api/wishlists/people");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var token = api.Services.GetRequiredService<ITokenService>().GenerateAccessToken(me);
        var response = await InMemoryApi.Send(client, HttpMethod.Get, "/api/wishlists/people", bearer: token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var people = await response.Content.ReadFromJsonAsync<List<ConnectedPersonDto>>();
        var person = Assert.Single(people!);
        Assert.Equal(anna.Id, person.UserId);
        Assert.Equal("Family", Assert.Single(person.Circles).Name);
    }
}
