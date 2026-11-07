using Wishlist.Domain.Enums;

namespace Wishlist.Application.Interfaces;

/// <summary>
/// Records sign-ins and active days for the admin page. Never throws: a failure
/// is logged and must not stop anyone signing in.
/// </summary>
public interface IActivityRecorder
{
    /// <summary>A new session started; also counts the day as active.</summary>
    Task RecordSignInAsync(Guid userId, SignInMethod method, CancellationToken ct = default);

    /// <summary>The user was active today (UTC); stored once per day.</summary>
    Task RecordActiveDayAsync(Guid userId, CancellationToken ct = default);
}
