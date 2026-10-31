using System.Net;
using System.Text.RegularExpressions;
using Wishlist.Application.UseCases.Wishlists;

namespace Wishlist.Application.UseCases.LegacyImport;

/// <summary>
/// Turns an old item into what a Kadoozle item holds: a title, a plain-text
/// description, and the web addresses found in the description as links.
/// </summary>
public static partial class LegacyItemMapper
{
    public const int MaxTitleLength = 300;

    public record Mapped(string Title, string? Description, IReadOnlyList<string> Urls);

    public static Mapped Map(string title, string? description)
    {
        var text = PlainText(description ?? string.Empty);

        var urls = new List<string>();
        foreach (Match m in Link().Matches(text))
        {
            var url = m.Value.TrimEnd('.', ',', ';', ':', ')', '!', '?', '\'', '"');
            if (url.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) url = "https://" + url;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                && !urls.Contains(url, StringComparer.OrdinalIgnoreCase)
                && urls.Count < WishlistItemUrlValidator.MaxUrls)
            {
                urls.Add(url);
            }
        }

        var rest = Link().Replace(text, " ");
        rest = Spaces().Replace(rest, " ");
        rest = BlankLines().Replace(rest, "\n\n");
        rest = string.Join("\n", rest.Split('\n').Select(line => line.Trim())).Trim();

        var cleanTitle = PlainText(title).Trim();
        if (cleanTitle.Length > MaxTitleLength) cleanTitle = cleanTitle[..MaxTitleLength];

        return new Mapped(cleanTitle, rest.Length == 0 ? null : rest, urls);
    }

    /// <summary>Removes the few HTML tags old descriptions contain and decodes entities.</summary>
    private static string PlainText(string html)
    {
        var text = LineBreakTag().Replace(html, "\n");
        text = Tag().Replace(text, string.Empty);
        return WebUtility.HtmlDecode(text).Replace("\r\n", "\n");
    }

    [GeneratedRegex(@"(?:https?://|www\.)[^\s<>""]+", RegexOptions.IgnoreCase)]
    private static partial Regex Link();

    [GeneratedRegex(@"<br\s*/?>|</p>", RegexOptions.IgnoreCase)]
    private static partial Regex LineBreakTag();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"[ \t]{2,}")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"\n\s*\n+")]
    private static partial Regex BlankLines();
}
