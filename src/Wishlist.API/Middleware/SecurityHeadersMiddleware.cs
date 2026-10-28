using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Wishlist.API.Middleware;

/// <summary>
/// Browser protections on every response: pages, static files, API answers and errors.
/// The Content-Security-Policy allows only this site's own resources; the one inline
/// script in index.html (the first-paint theme script) is allowed by its hash, taken
/// from the page actually served so it can't drift.
/// </summary>
public sealed partial class SecurityHeadersMiddleware(RequestDelegate next, ISpaTemplate spa, IWebHostEnvironment env)
{
    public const string HstsValue = "max-age=31536000";

    private readonly Lazy<string> _csp = new(() => ContentSecurityPolicy(spa.Html));

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.ContentSecurityPolicy = _csp.Value;
            headers.XFrameOptions = "DENY";
            headers.XContentTypeOptions = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            // Deployed, the scheme comes from the ingress's X-Forwarded-Proto.
            if (!env.IsDevelopment() && context.Request.IsHttps)
                headers.StrictTransportSecurity = HstsValue;
            return Task.CompletedTask;
        });
        return next(context);
    }

    public static string ContentSecurityPolicy(string? html)
    {
        var scriptHashes = InlineScriptHashes(html).Select(h => $" '{h}'");
        return string.Join("; ",
            "default-src 'self'",
            "script-src 'self'" + string.Concat(scriptHashes),
            // UI libraries add <style> tags (e.g. scroll lock in dialogs); styles can't run code.
            "style-src 'self' 'unsafe-inline'",
            "img-src 'self' data:",
            "font-src 'self'",
            "connect-src 'self'",
            "manifest-src 'self'",
            "worker-src 'self'",
            "object-src 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "frame-ancestors 'none'");
    }

    /// <summary>"sha256-…" for each inline &lt;script&gt; (one without a src) in the page.</summary>
    public static IEnumerable<string> InlineScriptHashes(string? html) =>
        html is null
            ? []
            : InlineScript().Matches(html)
                .Select(m => "sha256-" + Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(m.Groups[1].Value))))
                .Distinct();

    [GeneratedRegex(@"<script(?![^>]*\bsrc\s*=)[^>]*>([\s\S]*?)</script>", RegexOptions.IgnoreCase)]
    private static partial Regex InlineScript();
}
