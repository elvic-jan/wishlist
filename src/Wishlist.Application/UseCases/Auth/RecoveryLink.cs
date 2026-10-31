namespace Wishlist.Application.UseCases.Auth;

/// <summary>The account-recovery link: the SPA's /recover page with the email and a reset token.</summary>
public static class RecoveryLink
{
    /// <summary>How long a recovery (password-reset) token stays valid.</summary>
    public static readonly TimeSpan Lifespan = TimeSpan.FromMinutes(30);

    public static string Build(string baseUrl, string email, string token)
    {
        var trimmedBase = baseUrl.TrimEnd('/');
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = Uri.EscapeDataString(token);
        return $"{trimmedBase}/recover?email={encodedEmail}&token={encodedToken}";
    }
}
