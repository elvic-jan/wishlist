using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Wishlist.API.Middleware;

namespace Wishlist.API.Tests;

/// <summary>Every response carries the browser protections; the page's own inline script is allowed by hash.</summary>
public class SecurityHeaderTests : IClassFixture<SecurityHeaderTests.PageApi>
{
    private const string ThemeScript = "\n      document.documentElement.classList.add('theme-terracotta');\n    ";

    private readonly PageApi _api;

    public SecurityHeaderTests(PageApi api) => _api = api;

    private static string Sha256(string script) =>
        "sha256-" + Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(script)));

    [Theory]
    [InlineData("/")]                    // the SPA page
    [InlineData("/circles/123")]         // the SPA page through the fallback
    [InlineData("/manifest.webmanifest")] // a static file
    [InlineData("/api/wishlists/me")]    // API: 401 without a session
    [InlineData("/api/invitations/unknown-code")] // API error from the error handler
    public async Task Every_response_carries_the_headers(string path)
    {
        var response = await InMemoryApi.Send(_api.CreateClient(), HttpMethod.Get, path, ip: "198.51.100.60");

        var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.Contains("object-src 'none'", csp);
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("strict-origin-when-cross-origin", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Contains("camera=()", Assert.Single(response.Headers.GetValues("Permissions-Policy")));
    }

    [Fact]
    public async Task Only_the_pages_own_inline_script_is_allowed()
    {
        var response = await _api.CreateClient().GetAsync("/");

        var scriptSrc = Assert.Single(response.Headers.GetValues("Content-Security-Policy"))
            .Split("; ").Single(d => d.StartsWith("script-src "));
        Assert.Equal($"script-src 'self' '{Sha256(ThemeScript)}'", scriptSrc);
    }

    [Fact]
    public async Task Hsts_on_https_requests_when_deployed()
    {
        // Send adds X-Forwarded-Proto: https, as the ingress does.
        var viaIngress = await InMemoryApi.Send(_api.CreateClient(), HttpMethod.Get, "/", ip: "198.51.100.61");
        var plain = await _api.CreateClient().GetAsync("/");

        Assert.Equal(SecurityHeadersMiddleware.HstsValue, Assert.Single(viaIngress.Headers.GetValues("Strict-Transport-Security")));
        Assert.False(plain.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public void Hashes_cover_every_inline_script_in_the_real_page()
    {
        var page = File.ReadAllText(Path.Combine(RepoRoot(), "src", "Wishlist.Web", "index.html"));

        var hashes = SecurityHeadersMiddleware.InlineScriptHashes(page).ToList();

        Assert.Single(hashes); // the first-paint theme script; the module script has a src
        Assert.Contains("theme-", page);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Wishlist.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    /// <summary>The in-memory API serving a page with one inline script and one module script.</summary>
    public sealed class PageApi : InMemoryApi
    {
        private static readonly string Page =
            "<!doctype html><html><head><title>GiftCircles</title>" +
            $"<script>{ThemeScript}</script>" +
            "<script type=\"module\" crossorigin src=\"/assets/index-abc123.js\"></script>" +
            "</head><body><div id=\"root\"></div></body></html>";

        private readonly string _webRoot = Directory.CreateTempSubdirectory("giftcircles-csp-").FullName;

        public PageApi()
        {
            File.WriteAllText(Path.Combine(_webRoot, "index.html"), Page);
            File.WriteAllText(Path.Combine(_webRoot, "manifest.webmanifest"), "{}");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseWebRoot(_webRoot);
            builder.ConfigureTestServices(services => services.AddSingleton<ISpaTemplate>(new FixedTemplate(Page)));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try { Directory.Delete(_webRoot, recursive: true); } catch (IOException) { }
        }

        private sealed record FixedTemplate(string? Html) : ISpaTemplate;
    }
}
