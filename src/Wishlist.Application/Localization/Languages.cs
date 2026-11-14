using System.Globalization;
using Wishlist.Domain.Entities;

namespace Wishlist.Application.Localization;

/// <summary>The languages the app is offered in. Adding one: a code here and a dictionary in <see cref="Texts"/>.</summary>
public static class Languages
{
    public const string English = "en";
    public const string Dutch = "nl";

    /// <summary>English is the language texts are written in, and the fallback.</summary>
    public const string Default = English;

    public static readonly IReadOnlyList<string> Supported = [English, Dutch];

    /// <summary>The supported language of a tag such as "nl", "nl-BE" or "NL_be"; null when not offered.</summary>
    public static string? Parse(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;
        var primary = tag.Trim().Split('-', '_')[0].ToLowerInvariant();
        return Supported.Contains(primary) ? primary : null;
    }

    /// <summary>
    /// The first supported language of an Accept-Language header, in the header's
    /// order of preference ("fr-BE, nl;q=0.8, en;q=0.5" gives Dutch).
    /// </summary>
    public static string FromAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header)) return Default;
        var best = header.Split(',')
            .Select((part, index) =>
            {
                var pieces = part.Split(';');
                var quality = 1.0;
                foreach (var p in pieces.Skip(1))
                {
                    var kv = p.Trim();
                    if (kv.StartsWith("q=", StringComparison.OrdinalIgnoreCase)
                        && double.TryParse(kv[2..], NumberStyles.Float, CultureInfo.InvariantCulture, out var q))
                        quality = q;
                }
                return (Language: Parse(pieces[0]), quality, index);
            })
            .Where(x => x.Language is not null && x.quality > 0)
            .OrderByDescending(x => x.quality).ThenBy(x => x.index)
            .FirstOrDefault();
        return best.Language ?? Default;
    }

    public static CultureInfo Culture(string language) =>
        language == Dutch ? DutchCulture : EnglishCulture;

    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-GB");
    private static readonly CultureInfo DutchCulture = CultureInfo.GetCultureInfo("nl-BE");

    /// <summary>
    /// The language of an email: the recipient's saved one, else that of the
    /// person whose action sends it, else <paramref name="fallback"/> (the
    /// language of the request; English in a job).
    /// </summary>
    public static string ForEmail(User recipient, User? sender, string fallback) =>
        Parse(recipient.Language) ?? Parse(sender?.Language) ?? Parse(fallback) ?? Default;
}

/// <summary>The language of what is being handled now: the request's, or English outside a request (jobs).</summary>
public interface ILanguageContext
{
    string Language { get; }
}

public sealed class DefaultLanguageContext : ILanguageContext
{
    public string Language => Languages.Default;
}
