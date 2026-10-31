using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Entities;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Repositories;

public class UserRepository(UserManager<ApplicationUser> userManager) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        return user is null ? null : Map(user);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : Map(user);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        return await userManager.FindByEmailAsync(email) is not null;
    }

    public async Task<User> CreateAsync(string email, string displayName, string password, CancellationToken ct = default)
    {
        var appUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(appUser, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return Map(appUser);
    }

    public async Task<User> CreateWithoutPasswordAsync(Guid id, string email, string displayName, CancellationToken ct = default)
    {
        var appUser = new ApplicationUser
        {
            Id = id,
            Email = email,
            UserName = email,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(appUser);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        return Map(appUser);
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        return await userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<bool> ConfirmEmailAsync(Guid userId, string token, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;
        try
        {
            return (await userManager.ConfirmEmailAsync(user, token)).Succeeded;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public async Task MarkEmailConfirmedAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        if (user.EmailConfirmed) return;

        user.EmailConfirmed = true;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<bool> VerifyPasswordResetTokenAsync(Guid userId, string token, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;
        try
        {
            return await userManager.VerifyUserTokenAsync(
                user, userManager.Options.Tokens.PasswordResetTokenProvider,
                UserManager<ApplicationUser>.ResetPasswordTokenPurpose, token);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public async Task InvalidateSecurityTokensAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");
        var result = await userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task UpdateDisplayNameAsync(Guid userId, string displayName, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        user.DisplayName = displayName;
        await userManager.UpdateAsync(user);
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct = default)
    {
        var user = userId == Guid.Empty ? null : await userManager.FindByIdAsync(userId.ToString());
        if (user?.PasswordHash is null)
        {
            // No account, or one without a password: hash anyway, so the answer
            // takes as long as a wrong password and doesn't reveal the account.
            VerifyDummyPassword(password);
            return false;
        }
        return await userManager.CheckPasswordAsync(user, password);
    }

    private static readonly ApplicationUser DummyUser = new();
    private static string? _dummyHash;

    private void VerifyDummyPassword(string password)
    {
        var hasher = userManager.PasswordHasher;
        _dummyHash ??= hasher.HashPassword(DummyUser, Guid.NewGuid().ToString());
        hasher.VerifyHashedPassword(DummyUser, _dummyHash, password);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        return await userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded;
    }

    private static User Map(ApplicationUser u) => new()
    {
        Id = u.Id,
        Email = u.Email ?? string.Empty,
        DisplayName = u.DisplayName,
        CreatedAt = u.CreatedAt,
        EmailConfirmed = u.EmailConfirmed,
        HasPassword = u.PasswordHash is not null
    };
}
