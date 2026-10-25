namespace Wishlist.Infrastructure.Options;

/// <summary>Where passkeys belong and which pages may use them.</summary>
public class PasskeyConfigOptions
{
    public const string SectionName = "Passkeys";

    /// <summary>
    /// The WebAuthn relying-party ID. Empty means: the host of
    /// <c>App:PublicBaseUrl</c> without a leading <c>www.</c>, so passkeys made on
    /// <c>www.giftcircles.app</c> also work on <c>giftcircles.app</c>.
    /// </summary>
    public string? ServerDomain { get; set; }

    /// <summary>Origins allowed besides the one of <c>App:PublicBaseUrl</c>.</summary>
    public string[] AdditionalOrigins { get; set; } = [];

    public static string ResolveServerDomain(string? configured, string publicBaseUrl)
    {
        if (!string.IsNullOrWhiteSpace(configured)) return configured.Trim().ToLowerInvariant();
        var host = new Uri(publicBaseUrl).Host.ToLowerInvariant();
        return host.StartsWith("www.", StringComparison.Ordinal) ? host[4..] : host;
    }

    public static IReadOnlySet<string> ResolveAllowedOrigins(string publicBaseUrl, IEnumerable<string> additional) =>
        new[] { publicBaseUrl }.Concat(additional)
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Select(o => new Uri(o).GetLeftPart(UriPartial.Authority).ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
}
