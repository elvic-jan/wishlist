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
}
