using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;

namespace Wishlist.Application.UseCases.Auth;

/// <summary>
/// Part of the temporary first-account bootstrap: tells the SPA whether a
/// codeless sign-up form may be shown. Goes away together with the bootstrap.
/// </summary>
public class GetRegistrationStatusUseCase(IUserRepository users)
{
    public async Task<RegistrationStatusDto> ExecuteAsync(CancellationToken ct = default) =>
        new(InviteRequired: await users.AnyAsync(ct));
}
