using System.Collections.Concurrent;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.Localization;

/// <summary>
/// The server's texts in every language. Texts are written in English in the
/// code; the English text (with <c>{0}</c> placeholders) is the key of its
/// translations in <see cref="DutchTexts"/>. A text without a translation is
/// shown in English and remembered in <see cref="Untranslated"/>, which the
/// tests check.
/// </summary>
public static class Texts
{
    private static readonly ConcurrentDictionary<string, byte> Missing = new();

    /// <summary>English templates that were asked for in another language and had no translation.</summary>
    public static IReadOnlyCollection<string> Untranslated => Missing.Keys.ToList();

    /// <summary>The template in <paramref name="language"/>, placeholders untouched.</summary>
    public static string Template(string language, string english)
    {
        if (language != Languages.Dutch) return english;
        if (DutchTexts.Entries.TryGetValue(english, out var dutch)) return dutch;
        Missing.TryAdd(english, 0);
        return english;
    }

    /// <summary>The text in <paramref name="language"/> with its values filled in.</summary>
    public static string T(string language, string english, params object?[] arguments)
    {
        var template = Template(language, english);
        return arguments.Length == 0 ? template : string.Format(Languages.Culture(language), template, Localized(language, arguments));
    }

    /// <summary>A user-facing error in <paramref name="language"/>.</summary>
    public static string T(string language, DomainException error) =>
        T(language, error.Template, [.. error.Arguments]);

    private static object?[] Localized(string language, object?[] arguments) =>
        [.. arguments.Select(a => a is Term term ? Template(language, term.English) : a)];
}
