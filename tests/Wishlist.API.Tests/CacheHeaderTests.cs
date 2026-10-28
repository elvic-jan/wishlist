using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Wishlist.API.Tests;

/// <summary>
/// A release has to reach people: the page and the service worker are always
/// revalidated, the content-hashed assets are kept for good.
/// </summary>
public class CacheHeaderTests : IClassFixture<CacheHeaderTests.BuiltSpaApi>
{
    private readonly BuiltSpaApi _api;

    public CacheHeaderTests(BuiltSpaApi api) => _api = api;

    [Theory]
    [InlineData("/index.html", "no-cache")]
    [InlineData("/sw.js", "no-cache")]
    [InlineData("/assets/index-abc123.js", "public, max-age=31536000, immutable")]
    [InlineData("/circles/123", "no-cache")]
    [InlineData("/", "no-cache")]
    public async Task Cache_control_per_file(string path, string expected)
    {
        var response = await _api.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expected, response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Invite_page_is_never_stored()
    {
        var response = await InMemoryApi.Send(_api.CreateClient(), HttpMethod.Get, $"/join/{Guid.NewGuid():N}", ip: "198.51.100.40");

        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Other_static_files_are_left_alone()
    {
        var response = await _api.CreateClient().GetAsync("/manifest.webmanifest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Headers.CacheControl);
    }

    /// <summary>The in-memory API with a stand-in for the built SPA in its web root.</summary>
    public sealed class BuiltSpaApi : InMemoryApi
    {
        private const string Page = """
            <!doctype html><html><head><title>GiftCircles</title>
            <script type="module" crossorigin src="/assets/index-abc123.js"></script>
            </head><body><div id="root"></div></body></html>
            """;

        private readonly string _webRoot = Directory.CreateTempSubdirectory("giftcircles-webroot-").FullName;

        public BuiltSpaApi()
        {
            Directory.CreateDirectory(Path.Combine(_webRoot, "assets"));
            File.WriteAllText(Path.Combine(_webRoot, "index.html"), Page);
            File.WriteAllText(Path.Combine(_webRoot, "sw.js"), "// worker");
            File.WriteAllText(Path.Combine(_webRoot, "assets", "index-abc123.js"), "// app");
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
