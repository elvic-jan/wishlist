using Microsoft.AspNetCore.Identity;

namespace Wishlist.Infrastructure.Persistence;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
