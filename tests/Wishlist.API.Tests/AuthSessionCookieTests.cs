using System.Net;

namespace Wishlist.API.Tests;

/// <summary>
/// The refresh-token cookie is what lets a session survive a page reload:
/// it must be issued on register as well as login, and a dead one must be cleared.
/// </summary>
public class AuthSessionCookieTests : IClassFixture<InMemoryApi>
{
    private readonly InMemoryApi _api;

    public AuthSessionCookieTests(InMemoryApi api) => _api = api;

    [Fact]
    public async Task Register_sets_refresh_token_cookie()
    {
        var client = _api.CreateClient();

        var response = await Register(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = RefreshCookie(response);
        Assert.NotNull(cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_after_register_rotates_the_cookie()
    {
        var client = _api.CreateClient();
        var registered = await Register(client);
        var issued = CookieValue(RefreshCookie(registered)!);

        var refreshed = await InMemoryApi.Post(client, "/api/auth/refresh", cookie: issued);

        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var rotated = CookieValue(RefreshCookie(refreshed)!);
        Assert.NotEqual(issued, rotated);
    }

    [Fact]
    public async Task Refresh_with_unknown_token_returns_401_and_clears_cookie()
    {
        var client = _api.CreateClient();

        var response = await InMemoryApi.Post(client, "/api/auth/refresh", cookie: "not-a-real-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var cookie = RefreshCookie(response);
        Assert.NotNull(cookie);
        Assert.Contains("expires=Thu, 01 Jan 1970", cookie, StringComparison.OrdinalIgnoreCase);
    }

    private Task<HttpResponseMessage> Register(HttpClient client) =>
        InMemoryApi.Post(client, "/api/auth/register", new
        {
            email = $"{Guid.NewGuid()}@example.com", displayName = "Ann", password = "Passw0rd!",
            inviteCode = _api.Store.AddInvitation().InviteCode,
        });

    private static string? RefreshCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith("refreshToken=", StringComparison.Ordinal))
            : null;

    private static string CookieValue(string setCookie) => setCookie.Split(';')[0]["refreshToken=".Length..];
}
