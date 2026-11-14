using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Wishlist.Application.Interfaces;

namespace Wishlist.Application.Localization;

/// <summary>
/// Builds an email in one language, as plain text and HTML from the same
/// paragraphs. Paragraph templates are English (the translation key) with
/// <c>{0}</c> placeholders and <c>**bold**</c>; values are HTML-encoded in the
/// HTML body and never translated, so names and gift titles appear as entered.
/// </summary>
public sealed partial class EmailText(string language)
{
    private readonly StringBuilder _plain = new();
    private readonly StringBuilder _html = new();

    public string Language => language;

    /// <summary>A text in the email's language, for the subject or a value.</summary>
    public string T(string english, params object?[] arguments) => Texts.T(language, english, arguments);

    public string Date(DateTime utc) => utc.ToString("d MMMM yyyy", Languages.Culture(language));

    public EmailText Greeting(string displayName) => Paragraph("Hi {0},", displayName);

    public EmailText Paragraph(string english, params object?[] arguments)
    {
        var template = Texts.Template(language, english);
        _plain.Append(Format(template.Replace("**", ""), arguments)).Append("\r\n\r\n");
        var html = Bold().Replace(WebUtility.HtmlEncode(template), "<strong>$1</strong>");
        _html.Append("<p>").Append(Format(html, [.. arguments.Select(Encode)])).Append("</p>");
        return this;
    }

    /// <summary>A link on its own line: the label, then the address in plain text; a labelled link in HTML.</summary>
    public EmailText Link(string url, string english, params object?[] arguments)
    {
        var template = Texts.Template(language, english);
        _plain.Append(Format(template, arguments)).Append(":\r\n").Append(url).Append("\r\n\r\n");
        _html.Append("<p><a href=\"").Append(WebUtility.HtmlEncode(url)).Append("\">")
            .Append(Format(WebUtility.HtmlEncode(template), [.. arguments.Select(Encode)])).Append("</a></p>");
        return this;
    }

    /// <summary>A link shown as its own address.</summary>
    public EmailText Link(string url)
    {
        _plain.Append(url).Append("\r\n\r\n");
        var encoded = WebUtility.HtmlEncode(url);
        _html.Append("<p><a href=\"").Append(encoded).Append("\">").Append(encoded).Append("</a></p>");
        return this;
    }

    public EmailMessage Build(string to, string subject)
    {
        var thanks = Texts.Template(language, "Thanks,");
        return new EmailMessage(
            To: to,
            Subject: subject,
            PlainTextBody: _plain + thanks + "\r\nKadoozle",
            HtmlBody: _html + "<p>" + WebUtility.HtmlEncode(thanks) + "<br/>Kadoozle</p>");
    }

    private string Format(string template, object?[] arguments) =>
        arguments.Length == 0 ? template : string.Format(Languages.Culture(language), template, arguments);

    private static object? Encode(object? value) => value is string s ? WebUtility.HtmlEncode(s) : value;

    [GeneratedRegex(@"\*\*(.+?)\*\*")]
    private static partial Regex Bold();
}
