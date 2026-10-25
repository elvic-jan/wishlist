using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Wishlist.Application.Interfaces;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.API.Tests;

/// <summary>Runs only when <c>GIFTCIRCLES_TEST_DB</c> names a throwaway database (its name must end in <c>_e2e</c>).</summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public const string Variable = "GIFTCIRCLES_TEST_DB";

    public PostgresFactAttribute()
    {
        var connection = Environment.GetEnvironmentVariable(Variable);
        if (string.IsNullOrWhiteSpace(connection))
            Skip = $"Set {Variable} to a Postgres connection string for a database ending in _e2e.";
        else if (!new NpgsqlConnectionStringBuilder(connection).Database!.EndsWith("_e2e", StringComparison.Ordinal))
            Skip = $"{Variable} must point at a database whose name ends in _e2e; it is dropped and recreated.";
    }
}

/// <summary>
/// The real WebAuthn verification (Identity's PasskeyHandler), the passkey
/// table and every passkey endpoint, against Postgres, with a software
/// authenticator standing in for the browser.
/// </summary>
public class PasskeyPostgresEndToEndTests
{
    private const string Origin = "https://giftcircles.test";

    [PostgresFact]
    public async Task Passkeys_work_end_to_end()
    {
        await using var api = new PostgresApi(Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!);
        await api.ResetDatabaseAsync();
        var client = api.CreateClient();
        using var annPhone = new SoftwareAuthenticator();

        // 1. First account (bootstrap, no invite) signs up with a passkey.
        var ann = await SignUp(client, annPhone, "ann@example.test", "Ann", inviteCode: null);
        Assert.Equal(HttpStatusCode.OK, ann.Response.StatusCode);
        var annToken = await AccessToken(ann.Response);

        // 2. Sign in with it, twice: the stored signature counter moves on.
        Assert.Equal(HttpStatusCode.OK, (await SignIn(client, annPhone)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SignIn(client, annPhone)).StatusCode);

        // 3. A response whose counter didn't increase looks cloned and is refused.
        annPhone.Counter = 1;
        Assert.Equal(HttpStatusCode.BadRequest, (await SignIn(client, annPhone)).StatusCode);
        annPhone.Counter = 10;

        // 4. A response produced for another site is refused.
        Assert.Equal(HttpStatusCode.BadRequest, (await SignIn(client, annPhone, origin: "https://evil.test")).StatusCode);

        // 5. Ann creates a circle and invites Bob, who signs up with a passkey.
        var group = await (await InMemoryApi.Post(client, "/api/groups", new { name = "Family" }, bearer: annToken))
            .Content.ReadFromJsonAsync<JsonElement>();
        var groupId = group.GetProperty("id").GetString()!;
        var invite = await (await InMemoryApi.Post(client, $"/api/groups/{groupId}/invitations", bearer: annToken))
            .Content.ReadFromJsonAsync<JsonElement>();
        var code = invite.GetProperty("inviteUrl").GetString()!.Split("/join/")[1];

        using var bobPhone = new SoftwareAuthenticator();
        var bob = await SignUp(client, bobPhone, "bob@example.test", "Bob", code);
        Assert.Equal(HttpStatusCode.OK, bob.Response.StatusCode);
        var bobBody = await bob.Response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(groupId, bobBody.GetProperty("joinedGroupId").GetString());

        // 6. Ann adds her laptop, then removes the phone; only the laptop signs in now.
        using var annLaptop = new SoftwareAuthenticator();
        var addOptions = await Options(await InMemoryApi.Post(client, "/api/auth/passkeys/options", bearer: annToken));
        var added = await InMemoryApi.Post(client, "/api/auth/passkeys",
            new { credential = annLaptop.Create(addOptions.Options, Origin), name = "Laptop" },
            bearer: annToken, ceremony: addOptions.Ceremony);
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);

        var security = await (await InMemoryApi.Send(client, HttpMethod.Get, "/api/auth/security", bearer: annToken))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(security.GetProperty("hasPassword").GetBoolean());
        Assert.Equal(2, security.GetProperty("passkeys").GetArrayLength());
        Assert.Contains(security.GetProperty("passkeys").EnumerateArray(), p => p.GetProperty("name").GetString() == "Laptop");

        var removed = await InMemoryApi.Send(client, HttpMethod.Delete, $"/api/auth/passkeys/{annPhone.Id}", bearer: annToken);
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await SignIn(client, annPhone)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SignIn(client, annLaptop)).StatusCode);

        // The last passkey of a passwordless account stays.
        var lastOne = await InMemoryApi.Send(client, HttpMethod.Delete, $"/api/auth/passkeys/{annLaptop.Id}", bearer: annToken);
        Assert.Equal(HttpStatusCode.Conflict, lastOne.StatusCode);

        // 7. Recovery: nothing for an unconfirmed address; after confirming, the
        //    emailed link adds a passkey on a new phone and ends old sessions.
        var oldSession = InMemoryApi.SetCookieValue(ann.Response, "refreshToken")!;
        await InMemoryApi.Post(client, "/api/auth/forgot-password", new { email = "ann@example.test" });
        Assert.DoesNotContain(api.Emails, m => m.Subject.StartsWith("Get back", StringComparison.Ordinal));

        var confirm = api.Emails.Single(m => m.To == "ann@example.test" && m.Subject.StartsWith("Confirm", StringComparison.Ordinal));
        var (userId, confirmToken) = LinkParts(confirm, "/confirm-email?", "userId", "token");
        Assert.Equal(HttpStatusCode.NoContent,
            (await InMemoryApi.Post(client, "/api/auth/confirm-email", new { userId, token = confirmToken })).StatusCode);

        await InMemoryApi.Post(client, "/api/auth/forgot-password", new { email = "ann@example.test" });
        var recoveryMail = api.Emails.Single(m => m.Subject == "Get back into your GiftCircles account");
        var (email, token) = LinkParts(recoveryMail, "/recover?", "email", "token");

        using var annNewPhone = new SoftwareAuthenticator();
        var recoveryOptions = await Options(await InMemoryApi.Post(client, "/api/auth/recovery/passkey/options", new { email, token }));
        var recovered = await InMemoryApi.Post(client, "/api/auth/recovery/passkey",
            new { email, token, credential = annNewPhone.Create(recoveryOptions.Options, Origin), passkeyName = "New phone" },
            ceremony: recoveryOptions.Ceremony);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await InMemoryApi.Post(client, "/api/auth/refresh", cookie: oldSession)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SignIn(client, annNewPhone)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await InMemoryApi.Post(client, "/api/auth/recovery/passkey/options", new { email, token })).StatusCode);

        // 8. What landed in the database.
        await using var scope = api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WishlistDbContext>();
        Assert.Equal(3, await db.UserPasskeys.CountAsync()); // Ann: laptop + new phone (old phone removed); Bob: phone
        Assert.True((await db.Users.SingleAsync(u => u.Email == "ann@example.test")).EmailConfirmed);
    }

    private static async Task<(HttpResponseMessage Response, string Ceremony)> SignUp(
        HttpClient client, SoftwareAuthenticator authenticator, string email, string displayName, string? inviteCode)
    {
        var options = await Options(await InMemoryApi.Post(client, "/api/auth/passkey/register/options",
            new { email, displayName, inviteCode }));
        var response = await InMemoryApi.Post(client, "/api/auth/passkey/register",
            new { email, displayName, inviteCode, credential = authenticator.Create(options.Options, Origin), passkeyName = "Phone" },
            ceremony: options.Ceremony);
        return (response, options.Ceremony);
    }

    private static async Task<HttpResponseMessage> SignIn(HttpClient client, SoftwareAuthenticator authenticator, string origin = Origin)
    {
        var options = await Options(await InMemoryApi.Post(client, "/api/auth/passkey/login/options"));
        return await InMemoryApi.Post(client, "/api/auth/passkey/login",
            new { credential = authenticator.Get(options.Options, origin) }, ceremony: options.Ceremony);
    }

    private static async Task<(JsonElement Options, string Ceremony)> Options(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("options"), InMemoryApi.SetCookieValue(response, "passkeyCeremony")!);
    }

    private static async Task<string> AccessToken(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;

    private static (string, string) LinkParts(EmailMessage mail, string marker, string first, string second)
    {
        // Emails use \r\n whatever the OS, so split on both rather than Environment.NewLine.
        var query = mail.PlainTextBody.Split(marker)[1].Split('\r', '\n')[0];
        var parts = query.Split('&').Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));
        return (parts[first], parts[second]);
    }
}

/// <summary>The real API (Identity, EF, PasskeyHandler) on a throwaway Postgres database; emails are captured.</summary>
internal sealed class PostgresApi(string connectionString) : WebApplicationFactory<Program>
{
    public ConcurrentQueue<EmailMessage> Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.UseSetting("Jwt:Key", "test-signing-key-that-is-long-enough-for-hmac-sha256");
        builder.UseSetting("Smtp:Host", "smtp.example.test");
        builder.UseSetting("App:PublicBaseUrl", "https://www.giftcircles.test");
        builder.UseSetting("Passkeys:AdditionalOrigins:0", "https://giftcircles.test");

        builder.ConfigureTestServices(services =>
            services.AddSingleton<IEmailSender>(new CapturingSender(Emails)));
    }

    public async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WishlistDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    private sealed class CapturingSender(ConcurrentQueue<EmailMessage> sink) : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken ct = default)
        {
            sink.Enqueue(message);
            return Task.CompletedTask;
        }
    }
}
