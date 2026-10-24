namespace Wishlist.Application.Interfaces;

/// <summary>
/// Caps how many wrong invite codes one client IP may submit per window, so the
/// register and join endpoints cannot be used to guess codes. Only wrong guesses
/// consume budget; once it is spent the IP is blocked until the window ends.
/// </summary>
public interface IInviteCodeAttemptLimiter
{
    bool IsBlocked(string ipAddress);
    void RecordFailure(string ipAddress);
}
