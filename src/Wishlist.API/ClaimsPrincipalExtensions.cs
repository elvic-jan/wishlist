using System.Security.Claims;

namespace Wishlist.API;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in user's id, from the access token's subject.</summary>
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim missing."));
}
