using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

public record OwnerAccessResult(bool Created, Guid UserId, string RecoveryUrl, DateTime ExpiresAtUtc);

/// <summary>
/// Behind the "owner-access" job, run by someone who can reach the database.
/// Creates the account when none has the email (the only way to create an
/// account without an invitation, e.g. the first one on a fresh install), and
/// returns a recovery link for it. Nothing is emailed: the caller prints the
/// link, so this also works when email or sign-in is broken.
/// </summary>
public class OwnerAccessUseCase(IUserRepository users, IAppUrlProvider appUrls)
{
    public async Task<OwnerAccessResult> ExecuteAsync(string? email, string? name, CancellationToken ct = default)
    {
        email = email?.Trim();
        name = name?.Trim();
        if (string.IsNullOrEmpty(email))
            throw new DomainException("Pass --email <email address>.");
        InputLimits.Check(email, InputLimits.Email, "Email address");

        var user = await users.GetByEmailAsync(email, ct);
        var created = user is null;
        if (user is null)
        {
            if (string.IsNullOrEmpty(name))
                throw new DomainException($"No account has {email}. Pass --name <display name> to create it.");
            InputLimits.Check(name, InputLimits.DisplayName, "Name");

            user = await users.CreateWithoutPasswordAsync(Guid.NewGuid(), email, name, ct);
            // The operator typed the address, so it counts as proved.
            await users.MarkEmailConfirmedAsync(user.Id, ct);
        }

        var token = await users.GeneratePasswordResetTokenAsync(user.Id, ct);
        return new OwnerAccessResult(
            created,
            user.Id,
            RecoveryLink.Build(appUrls.PublicBaseUrl, user.Email, token),
            DateTime.UtcNow + RecoveryLink.Lifespan);
    }
}
