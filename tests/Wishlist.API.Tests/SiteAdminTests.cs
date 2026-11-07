using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Admin;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;
using Wishlist.Infrastructure.Persistence;
using Wishlist.Infrastructure.Services;

namespace Wishlist.API.Tests;

/// <summary>
/// The site admin page: only accounts with the Admin role can read it, and
/// sign-ins and active days are recorded for it.
/// </summary>
public class SiteAdminTests : IClassFixture<InMemoryApi>
{
    private const string Password = "Passw0rd!long";
    private readonly InMemoryApi _api;

    public SiteAdminTests(InMemoryApi api) => _api = api;

    private InMemoryStore Store => _api.Store;

    private async Task<User> Account(bool admin = false)
    {
        var user = await new InMemoryUsers(Store).CreateAsync($"{Guid.NewGuid():N}@example.test", "Ann", Password);
        user.IsAdmin = admin;
        return user;
    }

    private async Task<(string AccessToken, string RefreshToken, JsonElement User)> SignIn(HttpClient client, User user)
    {
        var response = await InMemoryApi.Post(client, "/api/auth/login", new { email = user.Email, password = Password },
            ip: $"198.51.100.{Random.Shared.Next(1, 250)}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("accessToken").GetString()!, InMemoryApi.SetCookieValue(response, "refreshToken")!,
            body.GetProperty("user"));
    }

    [Theory]
    [InlineData("/api/admin/overview")]
    [InlineData("/api/admin/users")]
    public async Task Without_a_token_admin_data_is_refused(string path)
    {
        var response = await InMemoryApi.Send(_api.CreateClient(), HttpMethod.Get, path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/admin/overview")]
    [InlineData("/api/admin/users")]
    public async Task A_signed_in_non_admin_gets_403(string path)
    {
        var client = _api.CreateClient();
        var (token, _, user) = await SignIn(client, await Account());

        var response = await InMemoryApi.Send(client, HttpMethod.Get, path, bearer: token);

        Assert.False(user.GetProperty("isAdmin").GetBoolean());
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/admin/overview")]
    [InlineData("/api/admin/users")]
    public async Task An_admin_reads_admin_data_and_it_is_not_stored(string path)
    {
        var client = _api.CreateClient();
        var (token, _, user) = await SignIn(client, await Account(admin: true));

        var response = await InMemoryApi.Send(client, HttpMethod.Get, path, bearer: token);

        Assert.True(user.GetProperty("isAdmin").GetBoolean());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Me_says_whether_the_user_is_admin()
    {
        var client = _api.CreateClient();
        var (adminToken, _, _) = await SignIn(client, await Account(admin: true));
        var (userToken, _, _) = await SignIn(client, await Account());

        var admin = await (await InMemoryApi.Send(client, HttpMethod.Get, "/api/auth/me", bearer: adminToken)).Content.ReadFromJsonAsync<JsonElement>();
        var other = await (await InMemoryApi.Send(client, HttpMethod.Get, "/api/auth/me", bearer: userToken)).Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(admin.GetProperty("isAdmin").GetBoolean());
        Assert.False(other.GetProperty("isAdmin").GetBoolean());
    }

    [Fact]
    public async Task Removing_the_role_applies_at_the_next_refresh()
    {
        var client = _api.CreateClient();
        var admin = await Account(admin: true);
        var (_, refresh, _) = await SignIn(client, admin);

        await new AdminRoleUseCase(new InMemoryUsers(Store)).ExecuteAsync(admin.Email, remove: true);
        var refreshed = await InMemoryApi.Post(client, "/api/auth/refresh", cookie: refresh);
        var token = (await refreshed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        var response = await InMemoryApi.Send(client, HttpMethod.Get, "/api/admin/overview", bearer: token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Password_sign_in_is_recorded_with_an_active_day()
    {
        var user = await Account();

        await SignIn(_api.CreateClient(), user);

        Assert.Equal(SignInMethod.Password, Assert.Single(Store.SignIns, s => s.UserId == user.Id).Method);
        Assert.True(Store.ActiveDays.ContainsKey((user.Id, DateOnly.FromDateTime(DateTime.UtcNow))));
    }

    [Fact]
    public async Task A_failed_sign_in_is_not_recorded()
    {
        var user = await Account();

        var response = await InMemoryApi.Post(_api.CreateClient(), "/api/auth/login",
            new { email = user.Email, password = "wrong" }, ip: "198.51.100.251");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(Store.SignIns, s => s.UserId == user.Id);
        Assert.DoesNotContain(Store.ActiveDays.Keys, k => k.UserId == user.Id);
    }

    [Fact]
    public async Task Registering_is_recorded_as_sign_up()
    {
        var email = $"{Guid.NewGuid():N}@example.test";
        var response = await InMemoryApi.Post(_api.CreateClient(), "/api/auth/register", new
        {
            email, displayName = "Ben", password = Password, inviteCode = Store.AddInvitation().InviteCode,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = Store.Users.Values.Single(u => u.Email == email);
        Assert.Equal(SignInMethod.SignUp, Assert.Single(Store.SignIns, s => s.UserId == user.Id).Method);
    }

    [Fact]
    public async Task A_refresh_counts_the_day_but_not_as_a_sign_in()
    {
        var client = _api.CreateClient();
        var user = await Account();
        var (_, refresh, _) = await SignIn(client, user);
        Store.ActiveDays.Clear();

        var response = await InMemoryApi.Post(client, "/api/auth/refresh", cookie: refresh);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(Store.SignIns, s => s.UserId == user.Id);
        Assert.True(Store.ActiveDays.ContainsKey((user.Id, DateOnly.FromDateTime(DateTime.UtcNow))));
    }

    // --- The admin job ---

    [Fact]
    public async Task Job_grants_reports_already_and_removes()
    {
        var user = await Account();
        var job = new AdminRoleUseCase(new InMemoryUsers(Store));

        var granted = await job.ExecuteAsync(user.Email, remove: false);
        var again = await job.ExecuteAsync(user.Email, remove: false);
        var removed = await job.ExecuteAsync(user.Email, remove: true);

        Assert.Equal((true, true), (granted.IsAdmin, granted.Changed));
        Assert.Equal((true, false), (again.IsAdmin, again.Changed));
        Assert.Equal((false, true), (removed.IsAdmin, removed.Changed));
        Assert.False(user.IsAdmin);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("nobody@example.test")]
    public async Task Job_fails_without_a_known_email(string? email)
    {
        var job = new AdminRoleUseCase(new InMemoryUsers(Store));
        await Assert.ThrowsAsync<DomainException>(() => job.ExecuteAsync(email, remove: false));
    }

    // --- Overview figures ---

    private static readonly DateTime Now = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

    private sealed class FixedClock(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
    }

    private sealed class FakeQueries(DateOnly? trackingSince, IReadOnlyList<DateTime> signUps) : IAdminQueries
    {
        public List<(DateOnly From, DateOnly To)> ActiveAsked { get; } = [];

        public Task<AdminTotals> GetTotalsAsync(DateTime nowUtc, CancellationToken ct = default) =>
            Task.FromResult(new AdminTotals(1, 2, 3, 4));
        public Task<IReadOnlyList<DateTime>> GetSignUpTimesSinceAsync(DateTime fromUtc, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DateTime>>(signUps.Where(t => t >= fromUtc).ToList());
        public Task<int> CountActiveUsersAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
        {
            ActiveAsked.Add((from, to));
            // Current periods end today; the earlier ones get a smaller number.
            return Task.FromResult(to == DateOnly.FromDateTime(Now) ? 12 : 9);
        }
        public Task<DateOnly?> GetTrackingSinceAsync(CancellationToken ct = default) => Task.FromResult(trackingSince);
        public Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<AdminUserDto>>([]);
    }

    [Fact]
    public async Task Sign_ups_per_month_cover_twelve_months_with_zeros()
    {
        var queries = new FakeQueries(null,
        [
            new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc), new(2026, 11, 19, 0, 0, 0, DateTimeKind.Utc),
            new(2026, 10, 31, 23, 59, 0, DateTimeKind.Utc),
            new(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            new(2025, 11, 30, 0, 0, 0, DateTimeKind.Utc), // 13 months back: left out
        ]);

        var overview = await new GetAdminOverviewUseCase(queries, new FixedClock(Now)).ExecuteAsync();

        Assert.Equal(12, overview.SignUpsByMonth.Count);
        Assert.Equal(new MonthlySignUpsDto(2025, 12, 1), overview.SignUpsByMonth[0]);
        Assert.Equal(new MonthlySignUpsDto(2026, 11, 2), overview.SignUpsByMonth[^1]);
        Assert.Equal(0, overview.SignUpsByMonth[5].Count);
        Assert.Equal((2, 1), (overview.SignUpsThisMonth, overview.SignUpsLastMonth));
        Assert.Equal(new AdminTotals(1, 2, 3, 4), overview.Totals);
    }

    [Fact]
    public async Task Active_users_are_compared_with_the_period_before()
    {
        var queries = new FakeQueries(new DateOnly(2026, 9, 1), []);

        var overview = await new GetAdminOverviewUseCase(queries, new FixedClock(Now)).ExecuteAsync();

        Assert.Equal(new ActiveUsersDto(7, 12, 9), overview.Active7);
        Assert.Equal(new ActiveUsersDto(30, 12, 9), overview.Active30);
        Assert.Contains((new DateOnly(2026, 11, 14), new DateOnly(2026, 11, 20)), queries.ActiveAsked);
        Assert.Contains((new DateOnly(2026, 11, 7), new DateOnly(2026, 11, 13)), queries.ActiveAsked);
        Assert.Contains((new DateOnly(2026, 10, 22), new DateOnly(2026, 11, 20)), queries.ActiveAsked);
        Assert.Contains((new DateOnly(2026, 9, 22), new DateOnly(2026, 10, 21)), queries.ActiveAsked);
    }

    [Fact]
    public async Task No_comparison_before_tracking_covers_the_previous_period()
    {
        // Covers the 7-day comparison (from Nov 7) but not the 30-day one (from Sep 22).
        var queries = new FakeQueries(new DateOnly(2026, 11, 1), []);

        var overview = await new GetAdminOverviewUseCase(queries, new FixedClock(Now)).ExecuteAsync();

        Assert.Equal(9, overview.Active7.Previous);
        Assert.Null(overview.Active30.Previous);
        Assert.Equal(new DateOnly(2026, 11, 1), overview.TrackingSince);
    }

    [Fact]
    public async Task Nothing_tracked_yet_gives_no_comparison()
    {
        var overview = await new GetAdminOverviewUseCase(new FakeQueries(null, []), new FixedClock(Now)).ExecuteAsync();

        Assert.Null(overview.Active7.Previous);
        Assert.Null(overview.TrackingSince);
    }

    [Fact]
    public async Task A_recording_failure_never_stops_the_sign_in()
    {
        var options = new DbContextOptionsBuilder<WishlistDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=none;Timeout=1").Options;
        await using var db = new WishlistDbContext(options);
        var recorder = new ActivityRecorder(db, TimeProvider.System, NullLogger<ActivityRecorder>.Instance);

        await recorder.RecordSignInAsync(Guid.NewGuid(), SignInMethod.Password);
        await recorder.RecordActiveDayAsync(Guid.NewGuid());
    }
}
