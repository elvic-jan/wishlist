using Microsoft.Extensions.Logging;
using Wishlist.Application.Interfaces;
using Wishlist.Domain;

namespace Wishlist.Application.UseCases.GiftClaims;

/// <summary>
/// The monthly reservation run: reminds reservers whose reservation ends at the
/// next run, then ends the reservations that are due and tells their reservers.
/// </summary>
public class ProcessReservationsUseCase(
    IGiftClaimRepository claims,
    IUserRepository users,
    ReservationEmails emails,
    TimeProvider time,
    ILogger<ProcessReservationsUseCase> logger)
{
    // The scheduler may start a little late (or, rarely, early); treat a run
    // within this window as the one on the hour.
    private static readonly TimeSpan RunTolerance = TimeSpan.FromHours(1);

    public async Task<ReservationRunResult> ExecuteAsync(CancellationToken ct = default)
    {
        var now = time.GetUtcNow().UtcDateTime + RunTolerance;

        // Expired first: a reservation whose reminder failed last month ends now,
        // and gets the "ended" email instead of a late reminder.
        var expired = await claims.GetExpiredAsync(now, ct);
        var expiredEmailed = 0;
        foreach (var notice in expired)
        {
            var (reserver, owner) = await PeopleAsync(notice, ct);
            if (reserver is not null && owner is not null && await emails.SendExpiredAsync(reserver, owner, notice, ct))
                expiredEmailed++;

            // Never keep an item blocked because an email failed.
            await claims.DeleteAsync(notice.ClaimId, ct);
        }

        var due = await claims.GetReminderDueAsync(now, now.AddMonths(ReservationSchedule.ReminderLeadMonths), ct);
        var reminded = 0;
        foreach (var notice in due)
        {
            var (reserver, owner) = await PeopleAsync(notice, ct);
            if (reserver is null || owner is null) continue;
            if (!await emails.SendReminderAsync(reserver, owner, notice, ct)) continue;

            await claims.MarkReminderSentAsync(notice.ClaimId, time.GetUtcNow().UtcDateTime, ct);
            reminded++;
        }

        var result = new ReservationRunResult(reminded, due.Count - reminded, expired.Count, expiredEmailed);
        logger.LogInformation(
            "Reservation run: {Reminded} reminders sent ({ReminderFailures} failed), {Expired} reservations ended ({ExpiredEmailed} emailed).",
            result.RemindersSent, result.RemindersFailed, result.Expired, result.ExpiredEmailed);
        return result;
    }

    private async Task<(Domain.Entities.User?, Domain.Entities.User?)> PeopleAsync(ReservationNotice notice, CancellationToken ct)
    {
        var reserver = await users.GetByIdAsync(notice.ClaimerId, ct);
        var owner = await users.GetByIdAsync(notice.OwnerId, ct);
        if (reserver is null || owner is null)
            logger.LogWarning("Reservation {ClaimId}: reserver or owner no longer exists.", notice.ClaimId);
        return (reserver, owner);
    }
}

public record ReservationRunResult(int RemindersSent, int RemindersFailed, int Expired, int ExpiredEmailed);
