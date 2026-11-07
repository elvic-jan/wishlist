using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Enums;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.Infrastructure.Services;

/// <summary>
/// Writes with plain SQL so nothing is left in the change tracker when a write
/// fails; the caller's own save is never affected.
/// </summary>
public class ActivityRecorder(WishlistDbContext db, TimeProvider time, ILogger<ActivityRecorder> logger) : IActivityRecorder
{
    public async Task RecordSignInAsync(Guid userId, SignInMethod method, CancellationToken ct = default)
    {
        var now = time.GetUtcNow().UtcDateTime;
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO "SignInEvents" ("Id", "UserId", "OccurredAt", "Method") VALUES ({Guid.NewGuid()}, {userId}, {now}, {method.ToString()})""",
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not record a sign-in for user {UserId}", userId);
        }
        await RecordActiveDayAsync(userId, ct);
    }

    public async Task RecordActiveDayAsync(Guid userId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO "UserActivityDays" ("UserId", "Day") VALUES ({userId}, {today}) ON CONFLICT DO NOTHING""",
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not record an active day for user {UserId}", userId);
        }
    }
}
