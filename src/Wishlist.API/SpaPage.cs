using System.Net;
using System.Text.RegularExpressions;

namespace Wishlist.API;

/// <summary>Where the SPA's built index.html comes from (wwwroot in a deployed container).</summary>
public interface ISpaTemplate
{
    /// <summary>The page, or null when the SPA isn't built into wwwroot.</summary>
    string? Html { get; }
}

public sealed class WebRootSpaTemplate(IWebHostEnvironment env) : ISpaTemplate
{
    private readonly Lazy<string?> _html = new(() =>
    {
        var file = env.WebRootFileProvider.GetFileInfo("index.html");
        if (!file.Exists) return null;
        using var reader = new StreamReader(file.CreateReadStream());
        return reader.ReadToEnd();
    });

    public string? Html => _html.Value;
}

/// <summary>
/// Serves the SPA page with link-preview (og:/twitter:) tags that crawlers can
/// read: image and URL made absolute for every page, and for invite links a
/// personal title. Crawlers don't run JavaScript, so this has to happen here.
/// </summary>
public static partial class SpaPage
{
    public sealed record Preview(string Title, string Description);

    public static string Render(string html, string baseUrl, string path, Preview? preview)
    {
        baseUrl = baseUrl.TrimEnd('/');
        html = SetMeta(html, "og:url", baseUrl + path);
        html = AbsoluteImage().Replace(html, m => m.Groups[1].Value + baseUrl + m.Groups[2].Value + m.Groups[3].Value);
        if (preview is not null)
        {
            html = SetMeta(html, "og:title", preview.Title);
            html = SetMeta(html, "twitter:title", preview.Title);
            html = SetMeta(html, "og:description", preview.Description);
            html = SetMeta(html, "twitter:description", preview.Description);
        }
        return html;
    }

    public static IResult Html(string html, bool noStore)
    {
        var page = Results.Text(html, "text/html; charset=utf-8");
        return noStore ? new NoStore(page) : page;
    }

    // Replaces the content of <meta property|name="key" content="...">.
    private static string SetMeta(string html, string key, string value) =>
        new Regex($"""(<meta\s+(?:property|name)="{Regex.Escape(key)}"\s+content=")[^"]*(")""")
            .Replace(html, m => m.Groups[1].Value + WebUtility.HtmlEncode(value) + m.Groups[2].Value, 1);

    [GeneratedRegex("""(<meta\s+(?:property|name)="(?:og|twitter):image"\s+content=")(/[^"]*)(")""")]
    private static partial Regex AbsoluteImage();

    private sealed class NoStore(IResult inner) : IResult
    {
        public Task ExecuteAsync(HttpContext http)
        {
            http.Response.Headers.CacheControl = "no-store";
            return inner.ExecuteAsync(http);
        }
    }
}
