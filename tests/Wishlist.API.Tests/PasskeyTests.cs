using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Wishlist.Domain.Entities;

namespace Wishlist.API.Tests;

/// <summary>
/// Passkey ceremonies through the API with a fake authenticator: the ceremony
/// cookie, the invite gate on sign-up, managing passkeys, recovery and email
/// confirmation. The real WebAuthn verification is covered by
/// <see cref="PasskeyPostgresEndToEndTests"/>.
/// </summary>
public class PasskeyTests : IClassFixture<InMemoryApi>
{
    private readonly InMemoryApi _api;
    private readonly HttpClient _client;

    public PasskeyTests(InMemoryApi api)
    {
        _api = api;
        _client = api.CreateClient();
    }

    // ---- sign-in -----------------------------------------------------------

    [Fact]
    public async Task Sign_in_with_a_registered_passkey_starts_a_session()
    {
        var user = _api.Store.AddPasskeyUser();
        var passkey = _api.Store.Passkeys.Values.Single(p => p.UserId == user.Id);

        var options = await Options("/api/auth/passkey/login/options");
        var response = await Post("/api/auth/passkey/login", new { credential = Credential(passkey.Id, options) },
            ceremony: options.Ceremony);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(user.Id.ToString(), body.GetProperty("user").GetProperty("id").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        Assert.NotNull(InMemoryApi.SetCookieValue(response, "refreshToken"));
        Assert.Equal(1, passkey.SignIns);
    }

    [Fact]
    public async Task Ceremony_cookie_is_httponly_secure_strict_scoped_and_short_lived()
    {
        var response = await Post("/api/auth/passkey/login/options");

        var cookie = InMemoryApi.SetCookieHeader(response, "passkeyCeremony")!;
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/auth", cookie, StringComparison.OrdinalIgnoreCase);
        var expires = DateTimeOffset.Parse(cookie.Split(';')
            .Select(p => p.Trim()).First(p => p.StartsWith("expires=", StringComparison.OrdinalIgnoreCase))[8..]);
        Assert.InRange(expires, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5).AddSeconds(5));
    }

    [Fact]
    public async Task Sign_in_without_a_ceremony_cookie_is_rejected()
    {
        var user = _api.Store.AddPasskeyUser();
        var options = await Options("/api/auth/passkey/login/options");
        var passkeyId = _api.Store.Passkeys.Values.Single(p => p.UserId == user.Id).Id;

        var response = await Post("/api/auth/passkey/login", new { credential = Credential(passkeyId, options) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(InMemoryApi.SetCookieValue(response, "refreshToken"));
    }

    [Fact]
    public async Task Sign_in_with_a_tampered_ceremony_cookie_is_rejected()
    {
        var user = _api.Store.AddPasskeyUser();
        var options = await Options("/api/auth/passkey/login/options");
        var passkeyId = _api.Store.Passkeys.Values.Single(p => p.UserId == user.Id).Id;

        var response = await Post("/api/auth/passkey/login", new { credential = Credential(passkeyId, options) },
            ceremony: options.Ceremony[..^4] + "AAAA");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_ceremony_can_be_completed_only_once()
    {
        var user = _api.Store.AddPasskeyUser();
        var passkeyId = _api.Store.Passkeys.Values.Single(p => p.UserId == user.Id).Id;
        var options = await Options("/api/auth/passkey/login/options");
        var body = new { credential = Credential(passkeyId, options) };

        var first = await Post("/api/auth/passkey/login", body, ceremony: options.Ceremony);
        var replay = await Post("/api/auth/passkey/login", body, ceremony: options.Ceremony);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Contains("expires=Thu, 01 Jan 1970", InMemoryApi.SetCookieHeader(first, "passkeyCeremony")!,
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
    }

    [Fact]
    public async Task A_ceremony_issued_for_sign_up_cannot_sign_in()
    {
        var user = _api.Store.AddPasskeyUser();
        var passkeyId = _api.Store.Passkeys.Values.Single(p => p.UserId == user.Id).Id;
        var signUp = await Options("/api/auth/passkey/register/options", new
        {
            email = NewEmail(), displayName = "Ann", inviteCode = _api.Store.AddInvitation().InviteCode,
        });

        var response = await Post("/api/auth/passkey/login", new { credential = Credential(passkeyId, signUp) },
            ceremony: signUp.Ceremony);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Sign_in_with_an_unknown_passkey_is_rejected()
    {
        var options = await Options("/api/auth/passkey/login/options");

        var response = await Post("/api/auth/passkey/login",
            new { credential = Credential(Guid.NewGuid().ToString("N"), options) }, ceremony: options.Ceremony);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Passkey sign-in failed.", body.GetProperty("error").GetString());
    }

    // ---- sign-up -----------------------------------------------------------

    [Fact]
    public async Task Passkey_sign_up_with_an_invite_creates_a_passwordless_member_and_sends_a_confirmation()
    {
        var invitation = _api.Store.AddInvitation();
        var email = NewEmail();
        var details = new { email, displayName = "Ann", inviteCode = invitation.InviteCode };

        var options = await Options("/api/auth/passkey/register/options", details);
        var credentialId = Guid.NewGuid().ToString("N");
        var response = await Post("/api/auth/passkey/register",
            new { email, displayName = "Ann", inviteCode = invitation.InviteCode,
                  credential = Credential(credentialId, options), passkeyName = "iPhone" },
            ceremony: options.Ceremony);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(invitation.GroupId.ToString(), body.GetProperty("joinedGroupId").GetString());
        var user = _api.Store.Users.Values.Single(u => u.Email == email);
        Assert.False(user.HasPassword);
        Assert.False(user.EmailConfirmed);
        Assert.True(_api.Store.IsMember(invitation.GroupId, user.Id));
        Assert.NotNull(invitation.UsedAt);
        Assert.Equal("iPhone", _api.Store.Passkeys[credentialId].Name);
        Assert.Equal(user.Id, _api.Store.Passkeys[credentialId].UserId);
        Assert.Contains(_api.Store.EmailsTo(email), m => m.Subject == "Confirm your GiftCircles email");
        Assert.NotNull(InMemoryApi.SetCookieValue(response, "refreshToken"));
    }

    [Fact]
    public async Task Passkey_sign_up_options_need_an_invite_once_accounts_exist()
    {
        _api.Store.AddUser();

        var response = await Post("/api/auth/passkey/register/options", new { email = NewEmail(), displayName = "Ann" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(InMemoryApi.SetCookieValue(response, "passkeyCeremony"));
    }

    [Fact]
    public async Task Passkey_sign_up_with_an_unknown_invite_is_404()
    {
        var response = await Post("/api/auth/passkey/register/options",
            new { email = NewEmail(), displayName = "Ann", inviteCode = Guid.NewGuid().ToString("N") },
            ip: "198.51.100.77");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Passkey_sign_up_rejects_details_that_changed_after_the_options()
    {
        var invitation = _api.Store.AddInvitation();
        var email = NewEmail();
        var options = await Options("/api/auth/passkey/register/options",
            new { email, displayName = "Ann", inviteCode = invitation.InviteCode });

        var otherEmail = NewEmail();
        var response = await Post("/api/auth/passkey/register",
            new { email = otherEmail, displayName = "Ann", inviteCode = invitation.InviteCode,
                  credential = Credential(Guid.NewGuid().ToString("N"), options) },
            ceremony: options.Ceremony);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(_api.Store.Users.Values, u => u.Email == email || u.Email == otherEmail);
    }

    [Fact]
    public async Task Passkey_sign_up_fails_if_the_email_was_taken_in_the_meantime()
    {
        var invitation = _api.Store.AddInvitation();
        var email = NewEmail();
        var options = await Options("/api/auth/passkey/register/options",
            new { email, displayName = "Ann", inviteCode = invitation.InviteCode });
        _api.Store.AddUser(email);

        var response = await Post("/api/auth/passkey/register",
            new { email, displayName = "Ann", inviteCode = invitation.InviteCode,
                  credential = Credential(Guid.NewGuid().ToString("N"), options) },
            ceremony: options.Ceremony);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single(_api.Store.Users.Values, u => u.Email == email);
    }

    // ---- managing passkeys -------------------------------------------------

    [Fact]
    public async Task Security_overview_lists_passkeys_and_account_flags()
    {
        var user = _api.Store.AddPasskeyUser(emailConfirmed: true);

        var response = await InMemoryApi.Send(_client, HttpMethod.Get, "/api/auth/security", bearer: Token(user));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("emailConfirmed").GetBoolean());
        Assert.False(body.GetProperty("hasPassword").GetBoolean());
        Assert.Equal(1, body.GetProperty("passkeys").GetArrayLength());
    }

    [Fact]
    public async Task Adding_a_passkey_needs_a_session()
    {
        var response = await Post("/api/auth/passkeys/options");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Adding_a_passkey_stores_it_and_emails_the_account()
    {
        var user = _api.Store.AddUser();
        var options = await Options("/api/auth/passkeys/options", bearer: Token(user));
        var credentialId = Guid.NewGuid().ToString("N");

        var response = await Post("/api/auth/passkeys",
            new { credential = Credential(credentialId, options), name = "Laptop" },
            bearer: Token(user), ceremony: options.Ceremony);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(user.Id, _api.Store.Passkeys[credentialId].UserId);
        Assert.Contains(_api.Store.EmailsTo(user.Email),
            m => m.Subject == "A passkey was added to your GiftCircles account" && m.PlainTextBody.Contains("Laptop"));
    }

    [Fact]
    public async Task Renaming_works_for_own_passkeys_only()
    {
        var owner = _api.Store.AddUser();
        var other = _api.Store.AddUser();
        var passkey = _api.Store.AddPasskeyFor(owner.Id);

        var own = await InMemoryApi.Send(_client, HttpMethod.Patch, $"/api/auth/passkeys/{passkey.Id}",
            new { name = "Work laptop" }, bearer: Token(owner));
        var foreign = await InMemoryApi.Send(_client, HttpMethod.Patch, $"/api/auth/passkeys/{passkey.Id}",
            new { name = "Mine now" }, bearer: Token(other));

        Assert.Equal(HttpStatusCode.NoContent, own.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal("Work laptop", passkey.Name);
    }

    [Fact]
    public async Task The_only_passkey_of_an_account_without_password_cannot_be_removed()
    {
        var user = _api.Store.AddPasskeyUser();
        var passkey = _api.Store.Passkeys.Values.Single(p => p.UserId == user.Id);

        var response = await InMemoryApi.Send(_client, HttpMethod.Delete, $"/api/auth/passkeys/{passkey.Id}",
            bearer: Token(user));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.True(_api.Store.Passkeys.ContainsKey(passkey.Id));
    }

    [Fact]
    public async Task A_passkey_can_be_removed_while_another_way_to_sign_in_remains()
    {
        var withPassword = _api.Store.AddUser();
        var onlyPasskey = _api.Store.AddPasskeyFor(withPassword.Id);
        var twoPasskeys = _api.Store.AddPasskeyUser();
        var second = _api.Store.AddPasskeyFor(twoPasskeys.Id);

        var a = await InMemoryApi.Send(_client, HttpMethod.Delete, $"/api/auth/passkeys/{onlyPasskey.Id}",
            bearer: Token(withPassword));
        var b = await InMemoryApi.Send(_client, HttpMethod.Delete, $"/api/auth/passkeys/{second.Id}",
            bearer: Token(twoPasskeys));

        Assert.Equal(HttpStatusCode.NoContent, a.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, b.StatusCode);
        Assert.False(_api.Store.Passkeys.ContainsKey(onlyPasskey.Id));
        Assert.False(_api.Store.Passkeys.ContainsKey(second.Id));
    }

    [Fact]
    public async Task Removing_another_accounts_passkey_is_404()
    {
        var owner = _api.Store.AddPasskeyUser();
        _api.Store.AddPasskeyFor(owner.Id);
        var passkey = _api.Store.Passkeys.Values.First(p => p.UserId == owner.Id);
        var other = _api.Store.AddUser();

        var response = await InMemoryApi.Send(_client, HttpMethod.Delete, $"/api/auth/passkeys/{passkey.Id}",
            bearer: Token(other));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(_api.Store.Passkeys.ContainsKey(passkey.Id));
    }

    [Fact]
    public async Task Signing_out_other_devices_keeps_the_current_session()
    {
        var user = _api.Store.AddPasskeyUser();
        var current = await SignInWithPasskey(user);
        var other = await SignInWithPasskey(user);

        var response = await Post("/api/auth/sessions/revoke-others", bearer: Token(user), cookie: current);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Post("/api/auth/refresh", cookie: current)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post("/api/auth/refresh", cookie: other)).StatusCode);
    }

    // ---- recovery ----------------------------------------------------------

    [Fact]
    public async Task No_recovery_email_for_a_passkey_only_account_with_an_unconfirmed_address()
    {
        var user = _api.Store.AddPasskeyUser(emailConfirmed: false);

        var response = await Post("/api/auth/forgot-password", new { email = user.Email }, ip: NewIp());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(_api.Store.EmailsTo(user.Email));
    }

    [Fact]
    public async Task Recovery_email_links_to_the_recover_page_for_eligible_accounts()
    {
        var withPassword = _api.Store.AddUser();
        var confirmedPasskeyOnly = _api.Store.AddPasskeyUser(emailConfirmed: true);

        await Post("/api/auth/forgot-password", new { email = withPassword.Email }, ip: NewIp());
        await Post("/api/auth/forgot-password", new { email = confirmedPasskeyOnly.Email }, ip: NewIp());

        foreach (var user in new[] { withPassword, confirmedPasskeyOnly })
        {
            var mail = Assert.Single(_api.Store.EmailsTo(user.Email));
            Assert.Equal("Get back into your GiftCircles account", mail.Subject);
            Assert.Contains("https://wishlist.example.test/recover?email=", mail.PlainTextBody);
        }
    }

    [Fact]
    public async Task Recovering_with_a_passkey_adds_it_confirms_the_email_and_signs_out_everywhere()
    {
        var user = _api.Store.AddUser();
        var oldSession = await RefreshTokenFor(user);
        await Post("/api/auth/forgot-password", new { email = user.Email }, ip: NewIp());
        var token = RecoveryToken(user);

        var options = await Options("/api/auth/recovery/passkey/options", new { email = user.Email, token });
        var credentialId = Guid.NewGuid().ToString("N");
        var response = await Post("/api/auth/recovery/passkey",
            new { email = user.Email, token, credential = Credential(credentialId, options), passkeyName = "New phone" },
            ceremony: options.Ceremony);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(InMemoryApi.SetCookieValue(response, "refreshToken"));
        Assert.Equal(user.Id, _api.Store.Passkeys[credentialId].UserId);
        Assert.True(user.EmailConfirmed);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post("/api/auth/refresh", cookie: oldSession)).StatusCode);

        // The link is used up.
        var again = await Post("/api/auth/recovery/passkey/options", new { email = user.Email, token });
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
    }

    [Fact]
    public async Task Recovery_with_a_bad_token_is_rejected()
    {
        var user = _api.Store.AddUser();

        var response = await Post("/api/auth/recovery/passkey/options", new { email = user.Email, token = "nope" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(InMemoryApi.SetCookieValue(response, "passkeyCeremony"));
    }

    [Fact]
    public async Task Resetting_the_password_confirms_the_email()
    {
        var user = _api.Store.AddPasskeyUser(emailConfirmed: true);
        user.EmailConfirmed = false;
        user.HasPassword = true;
        await Post("/api/auth/forgot-password", new { email = user.Email }, ip: NewIp());

        var response = await Post("/api/auth/reset-password",
            new { email = user.Email, token = RecoveryToken(user), newPassword = "a-new-password" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(user.EmailConfirmed);
    }

    // ---- email confirmation ------------------------------------------------

    [Fact]
    public async Task Password_sign_up_sends_a_confirmation_that_confirms_the_email()
    {
        var email = NewEmail();
        var registered = await Post("/api/auth/register", new
        {
            email, displayName = "Ann", password = "Passw0rd!", inviteCode = _api.Store.AddInvitation().InviteCode,
        });
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        var user = _api.Store.Users.Values.Single(u => u.Email == email);
        var (userId, token) = ConfirmationLink(Assert.Single(_api.Store.EmailsTo(email)));

        var wrong = await Post("/api/auth/confirm-email", new { userId, token = token + "x" });
        var right = await Post("/api/auth/confirm-email", new { userId, token });

        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, right.StatusCode);
        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public async Task Confirmation_can_be_resent_until_confirmed()
    {
        var user = _api.Store.AddUser();

        var first = await Post("/api/auth/confirm-email/resend", bearer: Token(user));
        user.EmailConfirmed = true;
        var second = await Post("/api/auth/confirm-email/resend", bearer: Token(user));

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
        Assert.Single(_api.Store.EmailsTo(user.Email));
    }

    // ---- helpers -----------------------------------------------------------

    private sealed record CeremonyOptions(string Ceremony, string Challenge);

    private async Task<CeremonyOptions> Options(string path, object? body = null, string? bearer = null)
    {
        var response = await Post(path, body, bearer: bearer);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new CeremonyOptions(
            InMemoryApi.SetCookieValue(response, "passkeyCeremony")!,
            json.GetProperty("options").GetProperty("challenge").GetString()!);
    }

    private static object Credential(string id, CeremonyOptions options) => new { id, challenge = options.Challenge };

    private Task<HttpResponseMessage> Post(string path, object? body = null, string? cookie = null,
        string? bearer = null, string ip = "203.0.113.1", string? ceremony = null) =>
        InMemoryApi.Post(_client, path, body, cookie, bearer, ip, ceremony);

    private async Task<string> SignInWithPasskey(User user)
    {
        var passkeyId = _api.Store.Passkeys.Values.First(p => p.UserId == user.Id).Id;
        var options = await Options("/api/auth/passkey/login/options");
        var response = await Post("/api/auth/passkey/login", new { credential = Credential(passkeyId, options) },
            ceremony: options.Ceremony);
        return InMemoryApi.SetCookieValue(response, "refreshToken")!;
    }

    private async Task<string> RefreshTokenFor(User user)
    {
        var tokens = _api.Services.GetRequiredService<Wishlist.Application.Interfaces.ITokenService>();
        return await tokens.CreateRefreshTokenAsync(user.Id);
    }

    private string Token(User user) =>
        _api.Services.GetRequiredService<Wishlist.Application.Interfaces.ITokenService>().GenerateAccessToken(user);

    private string RecoveryToken(User user)
    {
        var mail = _api.Store.EmailsTo(user.Email).Last();
        // Emails use \r\n whatever the OS, so split on both rather than Environment.NewLine.
        var query = mail.PlainTextBody.Split("?email=")[1].Split('\r', '\n')[0];
        var token = query.Split("&token=")[1].Trim();
        return Uri.UnescapeDataString(token);
    }

    private static (Guid UserId, string Token) ConfirmationLink(Wishlist.Application.Interfaces.EmailMessage mail)
    {
        var query = mail.PlainTextBody.Split("/confirm-email?")[1].Split('\r', '\n')[0];
        var parts = query.Split('&').Select(p => p.Split('=', 2)).ToDictionary(p => p[0], p => Uri.UnescapeDataString(p[1]));
        return (Guid.Parse(parts["userId"]), parts["token"]);
    }

    private static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

    private static string NewIp() => $"198.51.100.{Random.Shared.Next(1, 250)}";
}
