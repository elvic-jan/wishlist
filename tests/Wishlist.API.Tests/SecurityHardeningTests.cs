using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using JwtKeyCheck = Wishlist.Infrastructure.DependencyInjection;

namespace Wishlist.API.Tests;

/// <summary>
/// The second batch of the security review: length limits, invite links from the
/// configured address, the JWT key check and sign-in for unknown emails.
/// </summary>
public class SecurityHardeningTests : IDisposable
{
    private readonly InMemoryApi _api = new();
    private readonly HttpClient _client;

    public SecurityHardeningTests() => _client = _api.CreateClient();

    public void Dispose() => _api.Dispose();

    private string TokenFor(User user) => _api.Services.GetRequiredService<ITokenService>().GenerateAccessToken(user);

    private static async Task AssertBadRequestNaming(HttpResponseMessage response, int limit)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Contains($"at most {limit} characters", body?.Error);
    }

    [Fact]
    public async Task Overlong_display_name_at_sign_up_is_refused()
    {
        var response = await InMemoryApi.Post(_client, "/api/auth/register", new RegisterRequest(
            "new@example.test", new string('a', InputLimits.DisplayName + 1), "Long-enough-1", "code"));

        await AssertBadRequestNaming(response, InputLimits.DisplayName);
        Assert.Empty(_api.Store.Users);
    }

    [Fact]
    public async Task Overlong_email_at_sign_up_is_refused()
    {
        var email = new string('a', InputLimits.Email) + "@example.test";
        var response = await InMemoryApi.Post(_client, "/api/auth/register", new RegisterRequest(email, "Ann", "Long-enough-1", "code"));

        await AssertBadRequestNaming(response, InputLimits.Email);
    }

    [Fact]
    public async Task Profile_name_must_be_present_and_not_too_long()
    {
        var me = _api.Store.AddUser("me@example.test");
        var before = me.DisplayName;

        var tooLong = await InMemoryApi.Send(_client, HttpMethod.Patch, "/api/auth/me",
            new UpdateProfileRequest(new string('a', InputLimits.DisplayName + 1)), bearer: TokenFor(me));
        await AssertBadRequestNaming(tooLong, InputLimits.DisplayName);

        var empty = await InMemoryApi.Send(_client, HttpMethod.Patch, "/api/auth/me", new UpdateProfileRequest("  "), bearer: TokenFor(me));
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(before, _api.Store.Users[me.Id].DisplayName);

        var ok = await InMemoryApi.Send(_client, HttpMethod.Patch, "/api/auth/me", new UpdateProfileRequest(" Ann "), bearer: TokenFor(me));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("Ann", (await ok.Content.ReadFromJsonAsync<UserDto>())!.DisplayName);
    }

    [Fact]
    public async Task Overlong_circle_name_gives_400_not_500()
    {
        var me = _api.Store.AddUser("me@example.test");

        var response = await InMemoryApi.Post(_client, "/api/groups",
            new CreateGroupRequest(new string('a', InputLimits.CircleName + 1)), bearer: TokenFor(me));

        await AssertBadRequestNaming(response, InputLimits.CircleName);
        Assert.Empty(_api.Store.Groups);
    }

    [Theory]
    [InlineData(InputLimits.ItemTitle + 1, 0, 0, InputLimits.ItemTitle)]
    [InlineData(1, InputLimits.ItemDescription + 1, 0, InputLimits.ItemDescription)]
    [InlineData(1, 0, InputLimits.Url + 1, InputLimits.Url)]
    public async Task Overlong_item_fields_give_400_before_anything_is_stored(int title, int description, int url, int limit)
    {
        // The wishlist repository isn't faked here: reaching it would fail with 500.
        var me = _api.Store.AddUser("me@example.test");
        string[]? urls = url == 0 ? null : ["https://shop.example/" + new string('a', url - "https://shop.example/".Length)];

        var response = await InMemoryApi.Post(_client, "/api/wishlists/me/items",
            new AddWishlistItemRequest(new string('t', title), new string('d', description), urls), bearer: TokenFor(me));

        await AssertBadRequestNaming(response, limit);
    }

    [Fact]
    public async Task Invite_link_ignores_a_forged_host_header()
    {
        var me = _api.Store.AddUser("me@example.test");
        var family = _api.Store.AddGroup("Family");
        _api.Store.Members[(family.Id, me.Id)] = new GroupMember { GroupId = family.Id, UserId = me.Id };

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/groups/{family.Id}/invitations") { Content = JsonContent.Create(new { }) };
        request.Headers.Host = "evil.example";
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(me));
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invite = await response.Content.ReadFromJsonAsync<InvitationDto>();
        Assert.StartsWith("https://wishlist.example.test/join/", invite!.InviteUrl);
    }

    [Fact]
    public async Task Unknown_email_is_refused_like_a_wrong_password()
    {
        var known = _api.Store.AddUser("known@example.test");
        _api.Store.Passwords[known.Id] = "Right-password-1";

        var unknown = await InMemoryApi.Post(_client, "/api/auth/login", new LoginRequest("nobody@example.test", "Whatever-123"));
        var wrong = await InMemoryApi.Post(_client, "/api/auth/login", new LoginRequest("known@example.test", "Whatever-123"));

        Assert.False(unknown.IsSuccessStatusCode);
        Assert.Equal(wrong.StatusCode, unknown.StatusCode);
        Assert.Equal(await wrong.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(JwtKeyCheck.PlaceholderJwtKey)]
    [InlineData("too-short-key")]
    public void Production_refuses_a_weak_jwt_key(string key)
    {
        using var api = new ApiWithJwtKey(key);

        var error = Assert.ThrowsAny<Exception>(() => api.CreateClient());
        Assert.Contains("Jwt:Key", Flatten(error));
    }

    [Fact]
    public void A_long_random_key_is_accepted()
    {
        JwtKeyCheck.ValidateJwtKey("test-signing-key-that-is-long-enough-for-hmac-sha256");
        Assert.ThrowsAny<InvalidOperationException>(() => JwtKeyCheck.ValidateJwtKey(""));
    }

    private static string Flatten(Exception e) => e.InnerException is null ? e.Message : e.Message + " " + Flatten(e.InnerException);

    private sealed record ErrorBody(string Error);

    private sealed class ApiWithJwtKey(string key) : InMemoryApi
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Jwt:Key", key);
        }
    }
}
