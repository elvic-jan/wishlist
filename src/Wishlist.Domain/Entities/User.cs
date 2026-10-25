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
}
