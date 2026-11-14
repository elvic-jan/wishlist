namespace Wishlist.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>The user has proved they receive mail at <see cref="Email"/>.</summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>The account can sign in with a password (passkey-only accounts cannot).</summary>
    public bool HasPassword { get; set; }

    /// <summary>The account has the site-wide Admin role (granted only by the admin job).</summary>
    public bool IsAdmin { get; set; }

    /// <summary>The language the person chose ("en", "nl"); null until they have used the app since languages exist.</summary>
    public string? Language { get; set; }
}
