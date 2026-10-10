using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;

namespace Wishlist.API.Tests;

/// <summary>
/// The refresh-token cookie is what lets a session survive a page reload:
/// it must be issued on register as well as login, and a dead one must be cleared.
/// </summary>
public class AuthSessionCookieTests : IClassFixture<AuthSessionCookieTests.Factory>
{
    private readonly Factory _factory;

    public AuthSessionCookieTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Register_sets_refresh_token_cookie()
    {
        var client = _factory.CreateClient();

        var response = await Send(client, "/api/auth/register",
            new { email = $"{Guid.NewGuid()}@example.com", displayName = "Ann", password = "Passw0rd!" });

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
        var client = _factory.CreateClient();
        var registered = await Send(client, "/api/auth/register",
            new { email = $"{Guid.NewGuid()}@example.com", displayName = "Ann", password = "Passw0rd!" });
        var issued = CookieValue(RefreshCookie(registered)!);

        var refreshed = await Send(client, "/api/auth/refresh", cookie: issued);

        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var rotated = CookieValue(RefreshCookie(refreshed)!);
        Assert.NotEqual(issued, rotated);
    }

    [Fact]
    public async Task Refresh_with_unknown_token_returns_401_and_clears_cookie()
    {
        var client = _factory.CreateClient();

        var response = await Send(client, "/api/auth/refresh", cookie: "not-a-real-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var cookie = RefreshCookie(response);
        Assert.NotNull(cookie);
        Assert.Contains("expires=Thu, 01 Jan 1970", cookie, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<HttpResponseMessage> Send(HttpClient client, string path, object? body = null, string? cookie = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body ?? new { }),
        };
        request.Headers.Add("X-Forwarded-Proto", "https");
        if (cookie is not null) request.Headers.Add("Cookie", $"refreshToken={cookie}");
        return await client.SendAsync(request);
    }

    private static string? RefreshCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith("refreshToken=", StringComparison.Ordinal))
            : null;

    private static string CookieValue(string setCookie) => setCookie.Split(';')[0]["refreshToken=".Length..];

    public class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=unused;Database=unused");
            builder.UseSetting("Jwt:Key", "test-signing-key-that-is-long-enough-for-hmac-sha256");
            builder.UseSetting("Smtp:Host", "smtp.example.test");
            builder.UseSetting("App:PublicBaseUrl", "https://wishlist.example.test");

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IUserRepository, InMemoryUsers>();
                services.AddSingleton<ITokenService, InMemoryTokens>();
            });
        }
    }

    private sealed class InMemoryUsers : IUserRepository
    {
        private readonly ConcurrentDictionary<Guid, User> _users = new();

        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_users.GetValueOrDefault(id));

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(_users.Values.FirstOrDefault(u => u.Email == email));

        public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(_users.Values.Any(u => u.Email == email));

        public Task<User> CreateAsync(string email, string displayName, string password, CancellationToken ct = default)
        {
            var user = new User { Id = Guid.NewGuid(), Email = email, DisplayName = displayName, CreatedAt = DateTime.UtcNow };
            _users[user.Id] = user;
            return Task.FromResult(user);
        }

        public Task UpdateDisplayNameAsync(Guid userId, string displayName, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct = default) => Task.FromResult(true);
        public Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct = default) => Task.FromResult("reset");
        public Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken ct = default) => Task.FromResult(false);
    }

    private sealed class InMemoryTokens : ITokenService
    {
        private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new();

        public string GenerateAccessToken(User user) => $"access-{user.Id}";

        public Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, CancellationToken ct = default)
        {
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(), UserId = userId, Token = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(30),
            };
            _tokens[token.Token] = token;
            return Task.FromResult(token);
        }

        public Task<RefreshToken?> GetActiveRefreshTokenAsync(string token, CancellationToken ct = default) =>
            Task.FromResult(_tokens.TryGetValue(token, out var t) && t.IsActive ? t : null);

        public Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default)
        {
            if (_tokens.TryGetValue(token, out var t)) t.RevokedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken ct = default)
        {
            foreach (var t in _tokens.Values.Where(t => t.UserId == userId)) t.RevokedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }
    }
}
