using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;

namespace Wishlist.Application.UseCases.Auth;

public class EmailConfirmationUseCase(IUserRepository users, AccountEmails emails)
{
    /// <summary>Sends a new link unless the email is already confirmed.</summary>
    public async Task ResendAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || user.EmailConfirmed) return;
        await emails.SendEmailConfirmationAsync(user, ct);
    }

    /// <summary>Returns <c>false</c> for any failure, without saying which.</summary>
    public async Task<bool> ConfirmAsync(ConfirmEmailRequest request, CancellationToken ct = default)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Token))
            return false;

        var user = await users.GetByIdAsync(request.UserId, ct);
        if (user is null) return false;
        if (user.EmailConfirmed) return true;

        return await users.ConfirmEmailAsync(user.Id, request.Token, ct);
    }
}
