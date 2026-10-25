using Microsoft.AspNetCore.Identity;

namespace Wishlist.Infrastructure.Persistence;

/// <summary>
/// The Identity table layout this app uses. Version 3 adds AspNetUserPasskeys.
/// Identity reads it from its options at runtime; the design-time factory must
/// use the same value or migrations would describe a different model.
/// </summary>
public static class IdentitySchema
{
    public static readonly Version Version = IdentitySchemaVersions.Version3;
}
