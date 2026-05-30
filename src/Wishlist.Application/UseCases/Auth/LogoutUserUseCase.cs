using Wishlist.Application.Interfaces;

namespace Wishlist.Application.UseCases.Auth;

public class LogoutUserUseCase(ITokenService tokens)
{
    public async Task ExecuteAsync(string refreshToken, CancellationToken ct = default)
        => await tokens.RevokeRefreshTokenAsync(refreshToken, ct);
}
