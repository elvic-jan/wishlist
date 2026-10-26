namespace Wishlist.Application.Interfaces;

/// <summary>
/// Caps how many unknown old usernames one account may try per hour, so the
/// import preview can't be used to list the old site's usernames.
/// </summary>
public interface ILegacyUserNameAttemptLimiter
{
    bool IsBlocked(Guid userId);
    void RecordFailure(Guid userId);
}
