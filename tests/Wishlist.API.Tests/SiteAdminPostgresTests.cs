using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Admin;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.API.Tests;

/// <summary>Admin role, activity recording and admin figures against real Postgres.</summary>
[Collection(PostgresCollection.Name)]
public class SiteAdminPostgresTests
{
    private const string Password = "Passw0rd!long";

    private static PostgresApi Api() => new(Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!);

    private static async Task<User> NewUser(IServiceProvider services, string email) =>
        await services.GetRequiredService<IUserRepository>().CreateAsync(email, "Someone", Password);

    private static HttpRequestMessage Request(HttpMethod method, string path, object? body = null, string? bearer = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        request.Headers.Add("X-Forwarded-Proto", "https");
        if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return request;
    }

    [PostgresFact]
    public async Task Job_role_reaches_the_token_and_opens_the_admin_endpoints()
    {
        await using var api = Api();
        await api.ResetDatabaseAsync();
        User user;
        await using (var scope = api.Services.CreateAsyncScope())
        {
            user = await NewUser(scope.ServiceProvider, "admin@example.test");
            var granted = await scope.ServiceProvider.GetRequiredService<AdminRoleUseCase>().ExecuteAsync(user.Email, remove: false);
            Assert.True(granted.Changed);
        }

        var client = api.CreateClient();
        var login = await client.SendAsync(Request(HttpMethod.Post, "/api/auth/login", new { email = user.Email, password = Password }));
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("accessToken").GetString();

        Assert.True(body.GetProperty("user").GetProperty("isAdmin").GetBoolean());
        var overview = await client.SendAsync(Request(HttpMethod.Get, "/api/admin/overview", bearer: token));
        Assert.Equal(HttpStatusCode.OK, overview.StatusCode);
        var users = await (await client.SendAsync(Request(HttpMethod.Get, "/api/admin/users", bearer: token)))
            .Content.ReadFromJsonAsync<JsonElement>();
        var me = Assert.Single(users.EnumerateArray());
        Assert.True(me.GetProperty("isAdmin").GetBoolean());
        Assert.Equal(1, me.GetProperty("signIns").GetInt32());

        await using (var scope = api.Services.CreateAsyncScope())
        {
            var removed = await scope.ServiceProvider.GetRequiredService<AdminRoleUseCase>().ExecuteAsync(user.Email, remove: true);
            Assert.True(removed.Changed);
            Assert.False((await scope.ServiceProvider.GetRequiredService<IUserRepository>().GetByIdAsync(user.Id))!.IsAdmin);
        }
    }

    [PostgresFact]
    public async Task Many_activity_records_in_a_day_store_one_row_and_sign_ins_add_up()
    {
        await using var api = Api();
        await api.ResetDatabaseAsync();
        await using var scope = api.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var user = await NewUser(services, "busy@example.test");
        var recorder = services.GetRequiredService<IActivityRecorder>();

        await recorder.RecordSignInAsync(user.Id, SignInMethod.Password);
        await recorder.RecordSignInAsync(user.Id, SignInMethod.Passkey);
        for (var i = 0; i < 10; i++) await recorder.RecordActiveDayAsync(user.Id);

        var db = services.GetRequiredService<WishlistDbContext>();
        Assert.Equal(1, await db.UserActivityDays.CountAsync(a => a.UserId == user.Id));
        Assert.Equal(
            [SignInMethod.Passkey, SignInMethod.Password],
            await db.SignInEvents.Where(s => s.UserId == user.Id).Select(s => s.Method).OrderBy(m => m).ToListAsync());
    }

    [PostgresFact]
    public async Task Admin_figures_come_from_the_database()
    {
        await using var api = Api();
        await api.ResetDatabaseAsync();
        await using var scope = api.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<WishlistDbContext>();
        var anna = await NewUser(services, "anna@example.test");
        var ben = await NewUser(services, "ben@example.test");
        var carla = await NewUser(services, "carla@example.test");

        var group = new Group { Id = Guid.NewGuid(), Name = "Family", CreatedAt = DateTime.UtcNow };
        db.Groups.Add(group);
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = anna.Id, Role = GroupMemberRole.Owner });
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = ben.Id, Role = GroupMemberRole.Member });
        var reserved = new WishlistItem { Id = Guid.NewGuid(), OwnerId = anna.Id, Title = "Book", CreatedAt = DateTime.UtcNow };
        var expired = new WishlistItem { Id = Guid.NewGuid(), OwnerId = anna.Id, Title = "Mug", CreatedAt = DateTime.UtcNow };
        var bought = new WishlistItem { Id = Guid.NewGuid(), OwnerId = anna.Id, Title = "Pen", CreatedAt = DateTime.UtcNow };
        db.WishlistItems.AddRange(reserved, expired, bought);
        db.GiftClaims.AddRange(
            new GiftClaim { Id = Guid.NewGuid(), WishlistItemId = reserved.Id, ClaimedByUserId = ben.Id, Status = GiftClaimStatus.Reserved, ClaimedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30) },
            new GiftClaim { Id = Guid.NewGuid(), WishlistItemId = expired.Id, ClaimedByUserId = ben.Id, Status = GiftClaimStatus.Reserved, ClaimedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(-1) },
            new GiftClaim { Id = Guid.NewGuid(), WishlistItemId = bought.Id, ClaimedByUserId = ben.Id, Status = GiftClaimStatus.Bought, ClaimedAt = DateTime.UtcNow });
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.UserActivityDays.AddRange(
            new UserActivityDay { UserId = anna.Id, Day = today },
            new UserActivityDay { UserId = anna.Id, Day = today.AddDays(-1) },
            new UserActivityDay { UserId = ben.Id, Day = today.AddDays(-10) });
        db.SignInEvents.AddRange(
            new SignInEvent { Id = Guid.NewGuid(), UserId = anna.Id, OccurredAt = DateTime.UtcNow.AddDays(-3), Method = SignInMethod.Password },
            new SignInEvent { Id = Guid.NewGuid(), UserId = anna.Id, OccurredAt = DateTime.UtcNow.AddHours(-1), Method = SignInMethod.Passkey });
        await db.SaveChangesAsync();

        var queries = services.GetRequiredService<IAdminQueries>();
        var totals = await queries.GetTotalsAsync(DateTime.UtcNow);
        var users = await queries.ListUsersAsync();

        Assert.Equal(new Application.DTOs.AdminTotals(3, 1, 3, 1), totals);
        Assert.Equal(1, await queries.CountActiveUsersAsync(today.AddDays(-6), today));
        Assert.Equal(2, await queries.CountActiveUsersAsync(today.AddDays(-29), today));
        Assert.Equal(today.AddDays(-10), await queries.GetTrackingSinceAsync());
        Assert.Equal(3, (await queries.GetSignUpTimesSinceAsync(DateTime.UtcNow.AddDays(-1))).Count);

        var a = users.Single(u => u.Id == anna.Id);
        Assert.Equal((1, 2, today, true, false), (a.Circles, a.SignIns, a.LastActiveOn, a.HasPassword, a.HasPasskey));
        Assert.True(a.LastSignInAt > DateTime.UtcNow.AddHours(-2));
        var c = users.Single(u => u.Id == carla.Id);
        Assert.Equal((0, 0, (DateOnly?)null, (DateTime?)null), (c.Circles, c.SignIns, c.LastActiveOn, c.LastSignInAt));
    }

    [PostgresFact]
    public async Task Deleting_an_account_removes_its_activity()
    {
        await using var api = Api();
        await api.ResetDatabaseAsync();
        await using var scope = api.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var user = await NewUser(services, "gone@example.test");
        await services.GetRequiredService<IActivityRecorder>().RecordSignInAsync(user.Id, SignInMethod.Password);

        var db = services.GetRequiredService<WishlistDbContext>();
        await db.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();

        Assert.False(await db.SignInEvents.AnyAsync());
        Assert.False(await db.UserActivityDays.AnyAsync());
    }
}
