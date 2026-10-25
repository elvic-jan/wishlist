namespace Wishlist.Application.DTOs;

public record RegisterRequest(string Email, string DisplayName, string Password, string? InviteCode = null);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, UserDto User, Guid? JoinedGroupId = null);
public record RegistrationStatusDto(bool InviteRequired);
public record UpdateProfileRequest(string DisplayName);
public record UserDto(Guid Id, string Email, string DisplayName);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record PasskeySignUpOptionsRequest(string Email, string DisplayName, string? InviteCode = null);

/// <param name="CredentialJson">The browser's PublicKeyCredential, serialised to JSON.</param>
/// <param name="PasskeyName">A label for the new passkey, e.g. "iPhone".</param>
public record PasskeySignUpRequest(
    string Email, string DisplayName, string? InviteCode, string CredentialJson, string? PasskeyName = null);

public record PasskeyRecoveryOptionsRequest(string Email, string Token);
public record PasskeyRecoveryRequest(string Email, string Token, string CredentialJson, string? PasskeyName = null);
public record ConfirmEmailRequest(Guid UserId, string Token);

public record SecurityOverviewDto(
    bool EmailConfirmed, bool HasPassword, IReadOnlyList<Wishlist.Application.Interfaces.PasskeySummary> Passkeys);
