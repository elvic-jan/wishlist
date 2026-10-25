namespace Wishlist.API.Controllers;

/// <summary>The rotating refresh token, kept where page scripts can't read it.</summary>
public static class RefreshTokenCookie
{
    public const string Name = "refreshToken";

    public static void Set(HttpResponse response, string token) =>
        response.Cookies.Append(Name, token, Options(DateTimeOffset.UtcNow.AddDays(30)));

    public static void Clear(HttpResponse response) =>
        response.Cookies.Delete(Name, Options(expires: null));

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    private static CookieOptions Options(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = expires
    };
}
