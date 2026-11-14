using Wishlist.Application.Localization;

namespace Wishlist.API;

/// <summary>
/// The language of the current request, from its Accept-Language header. The
/// SPA sends the language it is showing, so this is the person's choice; a
/// crawler or plain browser request gives its own preference. English outside
/// a request (jobs).
/// </summary>
public sealed class RequestLanguage(IHttpContextAccessor accessor) : ILanguageContext
{
    public string Language => accessor.HttpContext is { } http ? Of(http) : Languages.Default;

    public static string Of(HttpContext http) =>
        Languages.FromAcceptLanguage(http.Request.Headers.AcceptLanguage.ToString());

    /// <summary>A text for this request's language; the English text is the key (see <see cref="Texts"/>).</summary>
    public static string T(HttpContext http, string english, params object?[] arguments) =>
        Texts.T(Of(http), english, arguments);
}
