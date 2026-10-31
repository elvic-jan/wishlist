using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Wishlist.Domain.Entities;

namespace Wishlist.API.Tests;

/// <summary>
/// The invitation check says who invited you to which circle, and invite links
/// carry a personal link preview that never confirms a guessed code.
/// </summary>
public class InviteSharingTests : IClassFixture<InviteSharingTests.PreviewApi>
{
    private const int GuessLimit = 10;
    private readonly PreviewApi _api;

    public InviteSharingTests(PreviewApi api)
    {
        _api = api;
        if (_api.Store.Users.IsEmpty) _api.Store.AddUser();
    }

    [Fact]
    public void New_links_work_for_three_days()
    {
        Assert.Equal(TimeSpan.FromDays(3), GroupInvitation.Lifetime);
    }

    [Fact]
    public async Task Check_names_the_inviter_and_the_circle()
    {
        var (invitation, _) = AddInvite("Jan", "Family");

        var response = await Get($"/api/invitations/{invitation.InviteCode}", "198.51.100.1");
        var body = await response.Content.ReadFromJsonAsync<Status>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(("Jan", "Family"), (body!.InviterName, body.CircleName));
        Assert.Null(invitation.UsedAt);
    }

    [Fact]
    public async Task Check_says_someone_when_the_inviter_is_gone()
    {
        var group = _api.Store.AddGroup("Friends");
        var invitation = _api.Store.AddInvitation(groupId: group.Id, createdBy: Guid.NewGuid());

        var body = await (await Get($"/api/invitations/{invitation.InviteCode}", "198.51.100.2")).Content.ReadFromJsonAsync<Status>();

        Assert.Equal(("Someone", "Friends"), (body!.InviterName, body.CircleName));
    }

    [Fact]
    public async Task Invite_link_has_a_personal_preview()
    {
        var (invitation, _) = AddInvite("Jan", "Family");

        var response = await Get($"/join/{invitation.InviteCode}", "198.51.100.3");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Contains("""<meta property="og:title" content="Jan invited you to Family on Kadoozle" />""", html);
        Assert.Contains("""<meta name="twitter:title" content="Jan invited you to Family on Kadoozle" />""", html);
        Assert.Contains("""<meta property="og:description" content="Join the circle to see each other&#39;s wishlists and coordinate gifts." />""", html);
        Assert.Contains($"""<meta property="og:url" content="https://wishlist.example.test/join/{invitation.InviteCode}" />""", html);
        Assert.Contains("""<meta property="og:image" content="https://wishlist.example.test/og/kadoozle.png" />""", html);
        Assert.Contains("""<div id="root"></div>""", html);
    }

    [Fact]
    public async Task Names_in_the_preview_are_html_encoded()
    {
        var (invitation, _) = AddInvite("<b>Jan</b>", "Tom & \"Jerry\"");

        var html = await (await Get($"/join/{invitation.InviteCode}", "198.51.100.4")).Content.ReadAsStringAsync();

        Assert.Contains("&lt;b&gt;Jan&lt;/b&gt; invited you to Tom &amp; &quot;Jerry&quot; on Kadoozle", html);
        Assert.DoesNotContain("<b>Jan</b>", html);
    }

    [Fact]
    public async Task Unknown_and_expired_links_get_the_generic_preview()
    {
        var (expired, _) = AddInvite("Jan", "Family", expiresAt: DateTime.UtcNow.AddMinutes(-1));

        foreach (var code in new[] { Guid.NewGuid().ToString("N"), expired.InviteCode, "not-a-code" })
        {
            var response = await Get($"/join/{code}", "198.51.100.5");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AssertGeneric(html);
        }
    }

    [Fact]
    public async Task Rate_limited_clients_get_the_generic_preview_even_for_a_valid_link()
    {
        const string ip = "198.51.100.6";
        var (invitation, _) = AddInvite("Jan", "Family");

        // Wrong codes through the preview count toward the same budget.
        for (var i = 0; i < GuessLimit; i++)
            await Get($"/join/{Guid.NewGuid():N}", ip);

        AssertGeneric(await (await Get($"/join/{invitation.InviteCode}", ip)).Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Get($"/api/invitations/{invitation.InviteCode}", ip)).StatusCode);
    }

    [Fact]
    public async Task Other_pages_get_the_generic_preview_with_absolute_urls()
    {
        var response = await Get("/circles/123", "198.51.100.7");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertGeneric(html);
        Assert.Contains("""<meta property="og:url" content="https://wishlist.example.test/circles/123" />""", html);
        Assert.Contains("""<meta name="twitter:image" content="https://wishlist.example.test/og/kadoozle.png" />""", html);
    }

    private static void AssertGeneric(string html)
    {
        Assert.Contains("""<meta property="og:title" content="Kadoozle" />""", html);
        Assert.Contains("""<meta property="og:image" content="https://wishlist.example.test/og/kadoozle.png" />""", html);
        Assert.DoesNotContain("invited you", html);
    }

    private (GroupInvitation Invitation, User Inviter) AddInvite(string inviterName, string circleName, DateTime? expiresAt = null)
    {
        var inviter = _api.Store.AddUser();
        inviter.DisplayName = inviterName;
        var group = _api.Store.AddGroup(circleName);
        return (_api.Store.AddInvitation(expiresAt, group.Id, inviter.Id), inviter);
    }

    private Task<HttpResponseMessage> Get(string path, string ip) =>
        InMemoryApi.Send(_api.CreateClient(), HttpMethod.Get, path, ip: ip);

    private sealed record Status(DateTime ExpiresAt, string InviterName, string CircleName);

    /// <summary>The in-memory API, serving a stand-in for the built SPA page.</summary>
    public sealed class PreviewApi : InMemoryApi
    {
        // The parts of src/Wishlist.Web/index.html the server rewrites.
        private const string Page = """
            <!doctype html><html><head>
            <title>Kadoozle</title>
            <meta property="og:title" content="Kadoozle" />
            <meta property="og:description" content="Share wishlists with your family and friends." />
            <meta property="og:url" content="/" />
            <meta property="og:image" content="/og/kadoozle.png" />
            <meta name="twitter:title" content="Kadoozle" />
            <meta name="twitter:description" content="Share wishlists with your family and friends." />
            <meta name="twitter:image" content="/og/kadoozle.png" />
            </head><body><div id="root"></div></body></html>
            """;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services => services.AddSingleton<ISpaTemplate>(new FixedTemplate(Page)));
        }

        private sealed record FixedTemplate(string? Html) : ISpaTemplate;
    }
}
