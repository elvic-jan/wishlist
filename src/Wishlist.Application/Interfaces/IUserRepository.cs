using Wishlist.Domain.Entities;

namespace Wishlist.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task<User> CreateAsync(string email, string displayName, string password, CancellationToken ct = default);

    /// <summary>
    /// Creates an account that signs in with a passkey only. The ID is chosen by
    /// the caller because the passkey was already bound to it during sign-up.
    /// </summary>
    Task<User> CreateWithoutPasswordAsync(Guid id, string email, string displayName, CancellationToken ct = default);

    /// <summary>Generates a token for the email-confirmation link.</summary>
    Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Confirms the email with a token from <see cref="GenerateEmailConfirmationTokenAsync"/>.
    /// Returns <c>false</c> for any failure; never throws for a bad token.
    /// </summary>
    Task<bool> ConfirmEmailAsync(Guid userId, string token, CancellationToken ct = default);

    /// <summary>Marks the email confirmed after the user proved control another way (e.g. a recovery link).</summary>
    Task MarkEmailConfirmedAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Checks a password-reset (recovery) token without redeeming it.
    /// Returns <c>false</c> for any failure.
    /// </summary>
    Task<bool> VerifyPasswordResetTokenAsync(Guid userId, string token, CancellationToken ct = default);

    /// <summary>Invalidates every outstanding reset and confirmation token of the user.</summary>
    Task InvalidateSecurityTokensAsync(Guid userId, CancellationToken ct = default);
    Task UpdateDisplayNameAsync(Guid userId, string displayName, CancellationToken ct = default);
    /// <summary>
    /// False for a wrong password, an unknown user (<see cref="Guid.Empty"/>) or an
    /// account without a password; all take about as long as a real check.
    /// </summary>
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
