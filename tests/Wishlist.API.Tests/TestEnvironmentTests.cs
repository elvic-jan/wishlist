using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.TestData;
using Wishlist.Infrastructure.Services;

namespace Wishlist.API.Tests;

/// <summary>
/// A test environment (staging) emails only allow-listed people, tells search
/// engines to stay away and names itself in /healthz; production does none of it.
/// </summary>
public class TestEnvironmentTests
{
    [Theory]
    [InlineData("tester@example.com", true)]
    [InlineData("Tester@Example.com", true)]
    [InlineData("tester+alice@example.com", true)]
    [InlineData("Tester+Alice@example.com", true)]
    [InlineData("anyone@example.org", true)]
    [InlineData("ANYONE+x@EXAMPLE.ORG", true)]
    [InlineData("exact+only@example.net", true)]
    [InlineData("exact@example.net", false)]
    [InlineData("exact+other@example.net", false)]
    [InlineData("other@example.com", false)]
    [InlineData("tester@example.com.evil.test", false)]
    [InlineData("tester@sub.example.org", false)]
    [InlineData("not-an-address", false)]
    public void Allow_list_matches_exact_plus_and_domain_entries(string recipient, bool allowed)
    {
        var list = EmailAllowList.Parse(" tester@example.com , *@example.org, exact+only@example.net ");

        Assert.Equal(allowed, list.Allows(recipient));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" , ")]
    public void An_empty_allow_list_allows_no_one(string? value)
    {
        var list = EmailAllowList.Parse(value);

        Assert.True(list.IsEmpty);
        Assert.False(list.Allows("tester@example.com"));
        Assert.Null(list.FirstPlainAddress);
    }

    [Fact]
    public void The_first_plain_address_is_the_test_users_inbox()
    {
        Assert.Equal("you@example.com", EmailAllowList.Parse("*@example.org, me+x@example.net, You@Example.com").FirstPlainAddress);
        Assert.Null(EmailAllowList.Parse("*@example.org, me+x@example.net").FirstPlainAddress);
    }

    [Theory]
    [InlineData("you+alice@gmail.com", "y**+alice@g****.com")]
    [InlineData("you@gmail.com", "y**@g****.com")]
    [InlineData("a@b.io", "a@b.io")]
    [InlineData("first.last+ben@mail.example.co.uk", "f*********+ben@m**************.uk")]
    [InlineData("nonsense", "***")]
    public void Recipients_are_masked_except_the_tag(string address, string masked)
    {
        Assert.Equal(masked, EmailAllowList.Mask(address));
    }

    [Fact]
    public async Task Listed_mail_is_sent_with_a_test_subject()
    {
        var inner = new RecordingSender();
        var sender = new AllowListEmailSender(inner, EmailAllowList.Parse("tester@example.com"), new ListLogger<AllowListEmailSender>());

        await sender.SendAsync(new EmailMessage("tester+anna@example.com", "Reset your password", "Open https://x/recover?t=secret"));

        var sent = Assert.Single(inner.Sent);
        Assert.Equal("tester+anna@example.com", sent.To);
        Assert.Equal("[TEST] Reset your password", sent.Subject);
    }

    [Fact]
    public async Task Unlisted_mail_is_dropped_and_logged_without_body_or_link()
    {
        var inner = new RecordingSender();
        var logger = new ListLogger<AllowListEmailSender>();
        var sender = new AllowListEmailSender(inner, EmailAllowList.Parse("tester@example.com"), logger);

        await sender.SendAsync(new EmailMessage("you+alice@gmail.com", "Reset your password",
            "Open https://x/recover?t=secret", "<a href=\"https://x/recover?t=secret\">Reset</a>"));

        Assert.Empty(inner.Sent);
        var line = Assert.Single(logger.Lines);
        Assert.Contains("Reset your password", line);
        Assert.Contains("y**+alice@g****.com", line);
        Assert.DoesNotContain("you+alice@gmail.com", line);
        Assert.DoesNotContain("secret", line);
    }

    [Fact]
    public async Task Without_an_allow_list_nothing_is_sent()
    {
        var inner = new RecordingSender();
        var sender = new AllowListEmailSender(inner, EmailAllowList.Parse(""), new ListLogger<AllowListEmailSender>());

        await sender.SendAsync(new EmailMessage("tester@example.com", "Hello", "Hi"));

        Assert.Empty(inner.Sent);
    }

    [Fact]
    public void Test_users_get_plus_addresses_of_the_inbox()
    {
        Assert.Equal("you+anna@example.com", SeedTestDataUseCase.AddressFor("you@example.com", "anna"));
    }

    [Fact]
    public async Task A_test_environment_marks_itself_and_filters_email()
    {
        await using var api = new HostApi(label: "TEST", allowList: "*@example.test");
        var client = api.CreateClient();

        var health = await client.GetAsync("/healthz");
        var page = await client.GetAsync("/");

        Assert.Equal("TEST", JsonDocument.Parse(await health.Content.ReadAsStringAsync()).RootElement.GetProperty("environment").GetString());
        Assert.Equal("noindex", Assert.Single(health.Headers.GetValues("X-Robots-Tag")));
        Assert.Equal("noindex", Assert.Single(page.Headers.GetValues("X-Robots-Tag")));
        Assert.IsType<AllowListEmailSender>(api.Services.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public async Task Production_is_unmarked_and_sends_email_directly()
    {
        await using var api = new HostApi(label: null, allowList: null);
        var client = api.CreateClient();

        var health = await client.GetAsync("/healthz");
        var page = await client.GetAsync("/");

        Assert.False(JsonDocument.Parse(await health.Content.ReadAsStringAsync()).RootElement.TryGetProperty("environment", out _));
        Assert.False(health.Headers.Contains("X-Robots-Tag"));
        Assert.False(page.Headers.Contains("X-Robots-Tag"));
        Assert.IsType<SmtpEmailSender>(api.Services.GetRequiredService<IEmailSender>());
    }

    /// <summary>The real host with its real email registration; no database is touched.</summary>
    private sealed class HostApi(string? label, string? allowList) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=unused;Database=unused");
            builder.UseSetting("Jwt:Key", "test-signing-key-that-is-long-enough-for-hmac-sha256");
            builder.UseSetting("Smtp:Host", "smtp.example.test");
            builder.UseSetting("App:PublicBaseUrl", "https://wishlist.example.test");
            if (label is not null) builder.UseSetting("App:EnvironmentLabel", label);
            // A domain entry only: no inbox for test users, so nothing is seeded.
            if (allowList is not null) builder.UseSetting("Smtp:AllowedRecipients", allowList);
        }
    }

    private sealed class RecordingSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken ct = default)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Lines { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Lines.Add(formatter(state, exception));
    }
}
