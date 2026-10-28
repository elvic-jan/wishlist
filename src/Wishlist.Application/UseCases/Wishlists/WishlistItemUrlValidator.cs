using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Wishlists;

/// <summary>
/// Validates a user-submitted URL list for a wishlist item. Returns the
/// cleaned (trimmed) list preserving the submitted order. Throws
/// <see cref="DomainException"/> on any violation so callers can rely on
/// non-throwing usage meaning "all URLs are good."
/// </summary>
internal static class WishlistItemUrlValidator
{
    public const int MaxUrls = 10;

    public static IReadOnlyList<string> Validate(IReadOnlyList<string>? urls)
    {
        if (urls is null || urls.Count == 0)
            return Array.Empty<string>();

        if (urls.Count > MaxUrls)
            throw new DomainException($"A wishlist item may have at most {MaxUrls} URLs.");

        var cleaned = new List<string>(urls.Count);
        foreach (var raw in urls)
        {
            var trimmed = raw?.Trim() ?? string.Empty;
            if (trimmed.Length == 0)
                throw new DomainException("URL must be an absolute http or https URL.");
            InputLimits.Check(trimmed, InputLimits.Url, "A link");

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new DomainException("URL must be an absolute http or https URL.");
            }

            cleaned.Add(trimmed);
        }

        return cleaned;
    }
}
