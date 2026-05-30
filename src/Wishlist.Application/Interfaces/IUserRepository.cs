using Wishlist.Domain.Entities;

namespace Wishlist.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<User> CreateAsync(string email, string displayName, string password, CancellationToken ct = default);
    Task UpdateDisplayNameAsync(Guid userId, string displayName, CancellationToken ct = default);
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken ct = default);
}
