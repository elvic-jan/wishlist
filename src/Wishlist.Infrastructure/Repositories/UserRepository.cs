using Microsoft.AspNetCore.Identity;
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

    public async Task UpdateDisplayNameAsync(Guid userId, string displayName, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        user.DisplayName = displayName;
        await userManager.UpdateAsync(user);
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;
        return await userManager.CheckPasswordAsync(user, password);
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
        CreatedAt = u.CreatedAt
    };
}
