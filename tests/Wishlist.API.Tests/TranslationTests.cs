using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Wishlist.Application.Localization;
using Wishlist.Domain.Entities;

namespace Wishlist.API.Tests;

/// <summary>
/// The app in English and Dutch: which language a request, an account and an
/// email get, and that every text the server shows to people has a Dutch
/// translation (openspec: translations).
/// </summary>
public partial class TranslationTests(InMemoryApi api) : IClassFixture<InMemoryApi>
{
    private const string Password = "Passw0rd!long";

    [Theory]
    [InlineData("nl", "nl")]
    [InlineData("nl-BE", "nl")]
    [InlineData("NL_be", "nl")]
    [InlineData("en-US", "en")]
    [InlineData("fr", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Language_tags_map_to_supported_languages(string? tag, string? expected) =>
        Assert.Equal(expected, Languages.Parse(tag));

    [Theory]
    [InlineData("nl-BE,nl;q=0.9,en;q=0.8", "nl")]
    [InlineData("fr-BE, nl;q=0.8, en;q=0.5", "nl")]
    [InlineData("en;q=0.4, nl;q=0.9", "nl")]
    [InlineData("fr, de;q=0.5", "en")]
    [InlineData("nl;q=0, en", "en")]
    [InlineData(null, "en")]
    public void Request_language_is_the_first_supported_preference(string? header, string expected) =>
        Assert.Equal(expected, Languages.FromAcceptLanguage(header));

    [Fact]
    public void Email_language_is_the_recipients_then_the_senders_then_the_fallback()
    {
        static User With(string? language) => new() { Language = language };

        Assert.Equal("nl", Languages.ForEmail(With("nl"), With("en"), "en"));
        Assert.Equal("nl", Languages.ForEmail(With(null), With("nl"), "en"));
        Assert.Equal("nl", Languages.ForEmail(With(null), With(null), "nl"));
        Assert.Equal("en", Languages.ForEmail(With(null), null, "en"));
        Assert.Equal("en", Languages.ForEmail(With("xx"), null, "yy"));
    }

    [Fact]
    public void Texts_fill_in_values_and_translate_terms()
    {
        var error = new Wishlist.Domain.Exceptions.DomainException(
            "{0} can be at most {1} characters.", new Wishlist.Domain.Exceptions.Term("Title"), 300);

        Assert.Equal("Title can be at most 300 characters.", error.Message);
        Assert.Equal("Title can be at most 300 characters.", Texts.T("en", error));
        Assert.Equal("De titel mag hoogstens 300 tekens lang zijn.", Texts.T("nl", error));
    }

    [Fact]
    public void Emails_keep_names_and_gift_titles_as_entered()
    {
        var mail = new EmailText("nl")
            .Greeting("Anna <b>")
            .Paragraph("Your reservation of **{0}** on {1}'s wishlist has ended, so others in the circle can reserve it now.",
                "Board game & more", "Tom");
        var message = mail.Build("anna@example.test", mail.T("Your reservation of \"{0}\" has ended", "Board game & more"));

        Assert.Equal("Je reservering van \"Board game & more\" is afgelopen", message.Subject);
        Assert.Contains("Hallo Anna <b>,", message.PlainTextBody);
        Assert.Contains("Je reservering van Board game & more op de verlanglijst van Tom", message.PlainTextBody);
        Assert.Contains("<strong>Board game &amp; more</strong>", message.HtmlBody);
        Assert.Contains("Hallo Anna &lt;b&gt;,", message.HtmlBody);
        Assert.EndsWith("Groeten,\r\nKadoozle", message.PlainTextBody);
    }

    [Fact]
    public void Email_dates_follow_the_language()
    {
        var date = new DateTime(2026, 11, 14, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal("14 November 2026", new EmailText("en").Date(date));
        Assert.Equal("14 november 2026", new EmailText("nl").Date(date));
    }

    [Fact]
    public async Task Errors_are_answered_in_the_request_language()
    {
        var client = api.CreateClient();
        var body = new { email = NewEmail(), displayName = "Ann", password = Password };

        var english = await Send(client, HttpMethod.Post, "/api/auth/register", body, language: null);
        var dutch = await Send(client, HttpMethod.Post, "/api/auth/register", body, language: "nl");

        Assert.Equal(HttpStatusCode.Forbidden, dutch.StatusCode);
        Assert.Equal("Registration is by invitation only.", await Error(english));
        Assert.Equal("Je kan alleen een account maken met een uitnodiging.", await Error(dutch));
    }

    [Fact]
    public async Task Registering_in_Dutch_saves_the_language_and_sends_a_Dutch_confirmation()
    {
        var client = api.CreateClient();
        var email = NewEmail();
        var invitation = api.Store.AddInvitation();

        var response = await Send(client, HttpMethod.Post, "/api/auth/register",
            new { email, displayName = "Ann", password = Password, inviteCode = invitation.InviteCode }, language: "nl-BE");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("nl", json.GetProperty("user").GetProperty("language").GetString());
        Assert.Equal("nl", api.Store.Users.Values.Single(u => u.Email == email).Language);
        var mail = Assert.Single(api.Store.EmailsTo(email));
        Assert.Equal("Bevestig je e-mailadres voor Kadoozle", mail.Subject);
        Assert.Contains("Hallo Ann,", mail.PlainTextBody);
    }

    [Fact]
    public async Task A_signed_in_person_can_save_their_language()
    {
        var client = api.CreateClient();
        var (token, user) = await RegisterAsync(client, language: null);
        Assert.Equal("en", user.Language);

        var saved = await Send(client, HttpMethod.Put, "/api/auth/me/language", new { language = "nl" }, bearer: token);
        var me = await Send(client, HttpMethod.Get, "/api/auth/me", bearer: token);

        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        Assert.Equal("nl", (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("language").GetString());
        Assert.Equal("nl", user.Language);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("nl-BE")]
    [InlineData("")]
    [InlineData(null)]
    public async Task An_unsupported_language_is_refused(string? language)
    {
        var client = api.CreateClient();
        var (token, user) = await RegisterAsync(client, language: "nl");

        var response = await Send(client, HttpMethod.Put, "/api/auth/me/language", new { language }, bearer: token, language: "nl");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Deze taal is niet beschikbaar.", await Error(response));
        Assert.Equal("nl", user.Language);
    }

    [Fact]
    public async Task Saving_a_language_needs_a_sign_in()
    {
        var response = await Send(api.CreateClient(), HttpMethod.Put, "/api/auth/me/language", new { language = "nl" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_reset_mail_uses_the_saved_language_before_the_request_language()
    {
        var client = api.CreateClient();
        var dutch = api.Store.AddUser(NewEmail(), language: "nl");
        var unset = api.Store.AddUser(NewEmail());

        await Send(client, HttpMethod.Post, "/api/auth/forgot-password", new { email = dutch.Email }, language: "en", ip: NewIp());
        await Send(client, HttpMethod.Post, "/api/auth/forgot-password", new { email = unset.Email }, language: "nl", ip: NewIp());
        await Send(client, HttpMethod.Post, "/api/auth/forgot-password", new { email = unset.Email }, language: "en", ip: NewIp());

        Assert.Equal("Opnieuw toegang tot je Kadoozle-account", Assert.Single(api.Store.EmailsTo(dutch.Email)).Subject);
        Assert.Equal(
            ["Opnieuw toegang tot je Kadoozle-account", "Get back into your Kadoozle account"],
            api.Store.EmailsTo(unset.Email).Select(m => m.Subject));
    }

    [Fact]
    public async Task The_invite_check_does_not_send_the_inviters_language()
    {
        var invitation = api.Store.AddInvitation();

        var response = await Send(api.CreateClient(), HttpMethod.Get, $"/api/groups/invitations/{invitation.InviteCode}");

        if (response.StatusCode == HttpStatusCode.OK)
            Assert.DoesNotContain("inviterLanguage", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    // ── Every user-facing server text has a Dutch translation ───────────────

    /// <summary>Texts only an operator sees (job output); they stay English.</summary>
    private static readonly HashSet<string> OperatorOnly =
    [
        "Pass --email <email address>.",
        "Pass --email <email> of the account.",
    ];

    [Fact]
    public void Every_user_facing_server_text_has_a_Dutch_translation()
    {
        var used = UserFacingTexts();

        Assert.True(used.Count > 80, $"Found only {used.Count} texts; the scan no longer matches the code.");
        var missing = used.Where(t => !DutchTexts.Entries.ContainsKey(t)).Order().ToList();
        Assert.True(missing.Count == 0, "No Dutch translation in DutchTexts for:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void Every_Dutch_translation_belongs_to_a_text_in_the_code()
    {
        var used = UserFacingTexts();

        var orphans = DutchTexts.Entries.Keys.Where(k => !used.Contains(k)).Order().ToList();
        Assert.True(orphans.Count == 0,
            "DutchTexts has entries whose English text is no longer in the code (changed wording?):\n" + string.Join("\n", orphans));
    }

    [Fact]
    public void Dutch_translations_keep_the_placeholders_of_the_English_text()
    {
        static IEnumerable<string> Marks(string text) =>
            Placeholder().Matches(text).Select(m => m.Value).Distinct().Order();

        foreach (var (english, dutch) in DutchTexts.Entries)
        {
            Assert.True(Marks(dutch).SequenceEqual(Marks(english)), $"Placeholders differ: \"{english}\" / \"{dutch}\"");
            Assert.Equal(english.Split("**").Length, dutch.Split("**").Length);
        }
    }

    /// <summary>
    /// The English texts the server shows to people, read from the source: error
    /// messages, email paragraphs and subjects, message definitions, and
    /// everything passed to <c>T(…)</c>.
    /// </summary>
    private static HashSet<string> UserFacingTexts()
    {
        var texts = new HashSet<string>();
        var src = Path.Combine(RepositoryRoot(), "src");
        var files = new[] { "Wishlist.API", "Wishlist.Application", "Wishlist.Domain", "Wishlist.Infrastructure" }
            .SelectMany(p => Directory.EnumerateFiles(Path.Combine(src, p), "*.cs", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}")
                && Path.GetFileName(f) != "DutchTexts.cs");

        foreach (var file in files)
        {
            // "a" + "b" over several lines is one text.
            var code = Concatenation().Replace(File.ReadAllText(file), "");
            foreach (var pattern in new[] { ErrorText(), TranslatedText(), EmailBodyText(), EmailLinkText(), LimitLabel(), MessageText(), NamedText() })
                foreach (Match m in pattern.Matches(code))
                    texts.Add(Regex.Unescape(m.Groups["text"].Value));
        }
        texts.ExceptWith(OperatorOnly);
        return texts;
    }

    private const string Literal = """(?<!\$)"(?<text>(?:[^"\\]|\\.)*)" """;

    [GeneratedRegex("""(?<=[^\\])"\s*\+\s*"(?!\s*\+)""")]
    private static partial Regex Concatenation();

    [GeneratedRegex("""new\s+(?:[\w.]+\.)?(?:Domain|NotFound|Forbidden|Conflict|TooManyRequests|Gone)Exception\(\s*(?<!\$)"(?<text>(?:[^"\\]|\\.)*)"|:\s*base\(\s*"(?<text>(?:[^"\\]|\\.)*)"\)""")]
    private static partial Regex ErrorText();

    [GeneratedRegex("""\bT\(\s*(?:[\w.]+\s*,\s*)?(?<!\$)"(?<text>(?:[^"\\]|\\.)*)" """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex TranslatedText();

    [GeneratedRegex("""\b(?:Paragraph|Template)\(\s*(?:[\w.]+\s*,\s*)?(?<!\$)"(?<text>(?:[^"\\]|\\.)*)" """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex EmailBodyText();

    [GeneratedRegex("""\.Link\(\s*[^,"]+,\s*(?<!\$)"(?<text>(?:[^"\\]|\\.)*)" """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex EmailLinkText();

    [GeneratedRegex("""InputLimits\.Check\([^;"]*,\s*"(?<text>(?:[^"\\]|\\.)*)"\)""")]
    private static partial Regex LimitLabel();

    [GeneratedRegex("""(?:string\s+(?:Title|Text)\s*=>|MessageAction\?\s+Action\s*=>\s*new\()\s*"(?<text>(?:[^"\\]|\\.)*)" """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex MessageText();

    [GeneratedRegex("""const\s+string\s+(?:CeremonyExpired|UnknownInviter)\s*=\s*"(?<text>(?:[^"\\]|\\.)*)" """, RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex NamedText();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();

    private static string RepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Wishlist.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<(string Token, User User)> RegisterAsync(HttpClient client, string? language)
    {
        var email = NewEmail();
        var invitation = api.Store.AddInvitation();
        var response = await Send(client, HttpMethod.Post, "/api/auth/register",
            new { email, displayName = "Ann", password = Password, inviteCode = invitation.InviteCode }, language: language);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (json.GetProperty("accessToken").GetString()!, api.Store.Users.Values.Single(u => u.Email == email));
    }

    private static async Task<HttpResponseMessage> Send(
        HttpClient client, HttpMethod method, string path, object? body = null,
        string? bearer = null, string? language = null, string ip = "203.0.113.77")
    {
        var request = new HttpRequestMessage(method, path);
        if (method != HttpMethod.Get) request.Content = JsonContent.Create(body ?? new { });
        request.Headers.Add("X-Forwarded-Proto", "https");
        request.Headers.Add("X-Forwarded-For", ip);
        if (language is not null) request.Headers.Add("Accept-Language", language);
        if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return await client.SendAsync(request);
    }

    private static async Task<string?> Error(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString();

    private static string NewEmail() => $"{Guid.NewGuid():N}@example.test";

    private static string NewIp() => $"198.51.100.{Random.Shared.Next(1, 250)}";
}
