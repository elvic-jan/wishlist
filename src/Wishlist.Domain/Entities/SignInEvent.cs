using Wishlist.Domain.Enums;

namespace Wishlist.Domain.Entities;

/// <summary>A new session was started (not a token refresh). No IP, browser or device is kept.</summary>
public class SignInEvent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime OccurredAt { get; set; }
    public SignInMethod Method { get; set; }
}
