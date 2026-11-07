namespace Wishlist.Domain.Entities;

/// <summary>The user used the app on this UTC day (signed in or renewed their session). One row per user per day.</summary>
public class UserActivityDay
{
    public Guid UserId { get; set; }
    public DateOnly Day { get; set; }
}
