namespace Wishlist.Application.DTOs;

public record RegisterRequest(string Email, string DisplayName, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, UserDto User);
public record UpdateProfileRequest(string DisplayName);
public record UserDto(Guid Id, string Email, string DisplayName);
