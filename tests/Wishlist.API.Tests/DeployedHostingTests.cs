using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.Interfaces;

namespace Wishlist.API.Tests;

/// <summary>
/// Runs the API as it runs in Azure: Production environment, plain HTTP inside
/// the container, scheme and client IP supplied by the ingress's forwarded headers.
/// </summary>
public class DeployedHostingTests : IClassFixture<DeployedHostingTests.ProductionFactory>
{
    private readonly ProductionFactory _factory;

    public DeployedHostingTests(ProductionFactory factory)
    {
        _factory = factory;
        _factory.RateLimiter.Reset();
    }

    [Fact]
    public async Task Healthz_returns_ok()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Auth_endpoint_over_plain_http_without_forwarded_proto_is_refused()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Auth_endpoint_with_forwarded_https_is_processed()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/forgot-password")
        {
            Content = JsonContent.Create(new { email = "" }),
        };
        request.Headers.Add("X-Forwarded-Proto", "https");

        var response = await client.SendAsync(request);

        // 400 comes from the action's own validation, so [RequireHttps] let it through.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Reset_rate_limit_is_keyed_on_forwarded_client_ip()
    {
        var client = _factory.CreateClient();

        await SendForgotPassword(client, "203.0.113.10");
        await SendForgotPassword(client, "198.51.100.20");

        Assert.Equal(["203.0.113.10", "198.51.100.20"], _factory.RateLimiter.SeenIps);
    }

    private static async Task SendForgotPassword(HttpClient client, string clientIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/forgot-password")
        {
            Content = JsonContent.Create(new { email = "someone@example.com" }),
        };
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-For", clientIp);

        var response = await client.SendAsync(request);

        // The recording limiter always denies, so no use case (and no database) runs.
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    public class ProductionFactory : WebApplicationFactory<Program>
    {
        public RecordingRateLimiter RateLimiter { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=unused;Database=unused");
            builder.UseSetting("Jwt:Key", "test-signing-key-that-is-long-enough-for-hmac-sha256");
            builder.UseSetting("Smtp:Host", "smtp.example.test");
            builder.UseSetting("App:PublicBaseUrl", "https://wishlist.example.test");

            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IForgotPasswordRateLimiter>(RateLimiter);
            });
        }
    }

    public class RecordingRateLimiter : IForgotPasswordRateLimiter
    {
        private readonly ConcurrentQueue<string> _ips = new();

        public IReadOnlyList<string> SeenIps => _ips.ToArray();

        public void Reset() => _ips.Clear();

        public ValueTask<bool> TryAcquireAsync(string email, string ipAddress, CancellationToken ct = default)
        {
            _ips.Enqueue(ipAddress);
            return ValueTask.FromResult(false);
        }
    }
}
