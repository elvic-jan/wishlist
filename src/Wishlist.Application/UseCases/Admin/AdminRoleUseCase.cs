using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Admin;

/// <summary>
/// Behind the "admin" job: grants or removes the site-wide Admin role. Run by
/// someone who can reach the database; the app itself can't make anyone admin.
/// </summary>
public class AdminRoleUseCase(IUserRepository users)
{
    public record Result(Guid UserId, bool IsAdmin, bool Changed);

    public async Task<Result> ExecuteAsync(string? email, bool remove, CancellationToken ct = default)
    {
        email = email?.Trim();
        if (string.IsNullOrEmpty(email))
            throw new DomainException("Pass --email <email> of the account.");

        var user = await users.GetByEmailAsync(email, ct)
            ?? throw new DomainException($"No account found for {email}.");

        var changed = await users.SetAdminAsync(user.Id, !remove, ct);
        return new Result(user.Id, !remove, changed);
    }
}
