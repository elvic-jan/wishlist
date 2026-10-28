namespace Wishlist.Application.Interfaces;

/// <summary>
/// Caps failed password sign-ins per email address and per client IP, so passwords
/// can't be guessed online. Only failures use up the budget; once either budget is
/// spent, password sign-in is refused until the window ends.
/// </summary>
public interface ILoginAttemptLimiter
{
    bool IsBlocked(string email, string ipAddress);
    void RecordFailure(string email, string ipAddress);
}
