using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Exceptions;

namespace Wishlist.Application.UseCases.Auth;

public class UpdateProfileUseCase(IUserRepository users)
{
    public async Task<UserDto> ExecuteAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var name = request.DisplayName?.Trim();
        if (string.IsNullOrEmpty(name))
            throw new DomainException("Name cannot be empty.");
        InputLimits.Check(name, InputLimits.DisplayName, "Name");

        await users.UpdateDisplayNameAsync(userId, name, ct);
        var user = await users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");
        return new UserDto(user.Id, user.Email, user.DisplayName);
    }
}
