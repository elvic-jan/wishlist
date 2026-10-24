using System.Net;
using System.Net.Http.Json;

namespace Wishlist.API.Tests;

/// <summary>
/// Accounts can only be created from a valid invite (which also joins its group),
/// except for the very first account on an empty system.
/// </summary>
public class InviteOnlyRegistrationTests : IClassFixture<InMemoryApi>
{
    private readonly InMemoryApi _api;

    public InviteOnlyRegistrationTests(InMemoryApi api)
    {
        _api = api;
        // These tests are about a system that is past bootstrap.
        if (_api.Store.Users.IsEmpty) _api.Store.AddUser();
    }

    [Fact]
    public async Task Register_without_code_is_forbidden_once_users_exist()
    {
        var email = NewEmail();

        var response = await Register(email, inviteCode: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("invitation only", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(_api.Store.Users.Values, u => u.Email == email);
    }

    [Fact]
    public async Task Unknown_code_is_404_and_creates_nothing()
    {
        var email = NewEmail();

        var response = await Register(email, Guid.NewGuid().ToString("N"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(_api.Store.Users.Values, u => u.Email == email);
    }

    [Fact]
    public async Task Expired_code_is_410_and_creates_nothing()
    {
        var email = NewEmail();
        var expired = _api.Store.AddInvitation(expiresAt: DateTime.UtcNow.AddMinutes(-1));

        var response = await Register(email, expired.InviteCode);

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.DoesNotContain(_api.Store.Users.Values, u => u.Email == email);
    }

    [Fact]
    public async Task Valid_code_creates_account_in_the_invited_group()
    {
        var invitation = _api.Store.AddInvitation();
        var email = NewEmail();

        var response = await Register(email, invitation.InviteCode);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("refreshToken="));
        var body = await response.Content.ReadFromJsonAsync<RegisterBody>();
        Assert.Equal(invitation.GroupId, body!.JoinedGroupId);
        var user = Assert.Single(_api.Store.Users.Values, u => u.Email == email);
        Assert.True(_api.Store.IsMember(invitation.GroupId, user.Id));
        Assert.NotNull(invitation.UsedAt);
    }

    [Fact]
    public async Task Same_code_registers_several_people_before_expiry()
    {
        var invitation = _api.Store.AddInvitation();

        var first = await Register(NewEmail(), invitation.InviteCode);
        var second = await Register(NewEmail(), invitation.InviteCode);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task Registered_email_without_valid_code_gets_the_invite_error_not_409()
    {
        var existing = _api.Store.AddUser();

        var noCode = await Register(existing.Email, inviteCode: null);
        var badCode = await Register(existing.Email, Guid.NewGuid().ToString("N"));
        var goodCode = await Register(existing.Email, _api.Store.AddInvitation().InviteCode);

        Assert.Equal(HttpStatusCode.Forbidden, noCode.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, badCode.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, goodCode.StatusCode);
    }

    [Fact]
    public async Task Failed_membership_rolls_back_the_new_account()
    {
        var invitation = _api.Store.AddInvitation();
        var email = NewEmail();
        _api.Store.FailNextAddMember = true;

        var response = await Register(email, invitation.InviteCode);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain(_api.Store.Users.Values, u => u.Email == email);
    }

    [Fact]
    public async Task Invitation_check_reports_valid_unknown_and_expired_codes_without_signing_in()
    {
        var valid = _api.Store.AddInvitation();
        var expired = _api.Store.AddInvitation(expiresAt: DateTime.UtcNow.AddMinutes(-1));

        var ok = await Check(valid.InviteCode);
        var unknown = await Check(Guid.NewGuid().ToString("N"));
        var gone = await Check(expired.InviteCode);

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Contains("expiresAt", await ok.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Gone, gone.StatusCode);
        Assert.Null(valid.UsedAt);
    }

    [Fact]
    public async Task Registration_status_requires_invite_once_users_exist()
    {
        var response = await InMemoryApi.Send(_api.CreateClient(), HttpMethod.Get, "/api/auth/registration");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<StatusBody>())!.InviteRequired);
    }

    private Task<HttpResponseMessage> Register(string email, string? inviteCode) =>
        InMemoryApi.Post(_api.CreateClient(), "/api/auth/register",
            new { email, displayName = "Ann", password = "Passw0rd!", inviteCode },
            ip: $"198.51.100.{Interlocked.Increment(ref _nextIp)}");

    // A fresh client IP per request keeps the wrong-guess limiter out of these tests.
    private static int _nextIp;

    private Task<HttpResponseMessage> Check(string code) =>
        InMemoryApi.Send(_api.CreateClient(), HttpMethod.Get, $"/api/invitations/{code}",
            ip: $"198.51.100.{Interlocked.Increment(ref _nextIp)}");

    private static string NewEmail() => $"{Guid.NewGuid()}@example.com";

    private sealed record RegisterBody(Guid? JoinedGroupId);
    private sealed record StatusBody(bool InviteRequired);
}

/// <summary>An empty system lets exactly one codeless sign-up through.</summary>
public class FirstAccountBootstrapTests : IDisposable
{
    private readonly InMemoryApi _api = new();

    [Fact]
    public async Task First_account_needs_no_code_and_closes_the_door_behind_it()
    {
        var client = _api.CreateClient();

        var before = await InMemoryApi.Send(client, HttpMethod.Get, "/api/auth/registration");
        var first = await Register(client);
        var after = await InMemoryApi.Send(client, HttpMethod.Get, "/api/auth/registration");
        var second = await Register(client);

        Assert.Contains("\"inviteRequired\":false", await before.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Contains("\"joinedGroupId\":null", await first.Content.ReadAsStringAsync());
        Assert.Empty(_api.Store.Members);
        Assert.Contains("\"inviteRequired\":true", await after.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Forbidden, second.StatusCode);
    }

    private static Task<HttpResponseMessage> Register(HttpClient client) =>
        InMemoryApi.Post(client, "/api/auth/register",
            new { email = $"{Guid.NewGuid()}@example.com", displayName = "Owner", password = "Passw0rd!" });

    public void Dispose() => _api.Dispose();
}
