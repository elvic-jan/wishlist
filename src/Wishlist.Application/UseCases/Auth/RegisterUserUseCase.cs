using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

public class RegisterUserUseCase(IUserRepository users, ITokenService tokens)
{
    public async Task<AuthResponse> ExecuteAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.DisplayName) ||
            request.Password.Length < 8)
            throw new DomainException("Invalid registration data.");

        if (await users.EmailExistsAsync(request.Email, ct))
            throw new ConflictException("An account with this email already exists.");

        var user = await users.CreateAsync(request.Email, request.DisplayName, request.Password, ct);
        var accessToken = tokens.GenerateAccessToken(user);
        await tokens.CreateRefreshTokenAsync(user.Id, ct);

        return new AuthResponse(accessToken, new UserDto(user.Id, user.Email, user.DisplayName));
    }
}
