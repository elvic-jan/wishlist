namespace Wishlist.Domain.Entities;

/// <summary>
/// A username from the old wishlist site (verlanglijstje). Only the username is
/// kept; the claim records which GiftCircles account imported it.
/// </summary>
public class LegacyUser
{
    /// <summary>The old site's USER_ID.</summary>
    public int LegacyId { get; set; }

    /// <summary>Upper case, as on the old site.</summary>
    public string UserName { get; set; } = string.Empty;

    public Guid? ClaimedByUserId { get; set; }
    public DateTime? ClaimedAt { get; set; }
    public int? ImportedItemCount { get; set; }

    public static string Normalize(string userName) => userName.Trim().ToUpperInvariant();
}
