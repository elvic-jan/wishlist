using Microsoft.AspNetCore.Identity;

namespace Wishlist.Infrastructure.Persistence;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>The chosen language ("en", "nl"); null until the person has used the app since languages exist.</summary>
    public string? Language { get; set; }
}
