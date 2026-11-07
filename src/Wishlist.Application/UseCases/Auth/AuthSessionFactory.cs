using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;

namespace Wishlist.Application.UseCases.Auth;

/// <summary>
/// Every way of signing in ends here: a short-lived access token plus a refresh
/// token, and the sign-in is recorded for the admin page.
/// </summary>
public class AuthSessionFactory(ITokenService tokens, IActivityRecorder activity)
{
    public async Task<(AuthResponse Response, string RefreshToken)> CreateAsync(
        User user, SignInMethod method, Guid? joinedGroupId = null, CancellationToken ct = default)
    {
        var accessToken = tokens.GenerateAccessToken(user);
        var refreshToken = await tokens.CreateRefreshTokenAsync(user.Id, ct);
        await activity.RecordSignInAsync(user.Id, method, ct);
        return (new AuthResponse(accessToken, UserDto.From(user), joinedGroupId), refreshToken);
    }
}
