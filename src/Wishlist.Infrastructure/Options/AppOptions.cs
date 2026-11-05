namespace Wishlist.Infrastructure.Options;

/// <summary>
/// App-level options. Currently exposes the public origin used to build
/// outbound links (password-reset emails, etc.).
/// </summary>
public class AppOptions
{
    public const string SectionName = "App";

    /// <summary>
    /// The HTTPS origin under which the SPA is served, e.g.
    /// <c>https://localhost:5173</c> in dev or <c>https://app.example.com</c>
    /// in production. MUST be HTTPS in non-Development environments.
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Marks a test environment, e.g. <c>TEST</c> on staging: a banner on every
    /// page, no search indexing, email only to <see cref="SmtpOptions.AllowedRecipients"/>,
    /// and seeded test data. Empty in production.
    /// </summary>
    public string? EnvironmentLabel { get; set; }

    public static string? EnvironmentLabelFrom(Microsoft.Extensions.Configuration.IConfiguration config) =>
        config[$"{SectionName}:{nameof(EnvironmentLabel)}"] is { } label && !string.IsNullOrWhiteSpace(label)
            ? label.Trim()
            : null;
}
