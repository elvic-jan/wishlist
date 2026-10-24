namespace Wishlist.Application.DTOs;

public record RegisterRequest(string Email, string DisplayName, string Password, string? InviteCode = null);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, UserDto User, Guid? JoinedGroupId = null);
public record RegistrationStatusDto(bool InviteRequired);
public record UpdateProfileRequest(string DisplayName);
public record UserDto(Guid Id, string Email, string DisplayName);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
