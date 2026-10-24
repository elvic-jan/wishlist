using Wishlist.Domain.Entities;

namespace Wishlist.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task<User> CreateAsync(string email, string displayName, string password, CancellationToken ct = default);
    Task UpdateDisplayNameAsync(Guid userId, string displayName, CancellationToken ct = default);
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct = default);

    /// <summary>
    /// Generates a single-use, time-limited password-reset token bound to the user.
    /// </summary>
    Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Resets the user's password using a token previously issued by
    /// <see cref="GeneratePasswordResetTokenAsync"/>. Returns <c>true</c> on success
    /// and <c>false</c> for any failure (wrong/expired/replayed token, password
    /// policy violation, user not found). Never throws for predictable controller
    /// behavior.
    /// </summary>
    Task<bool> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken ct = default);
}
