namespace Wishlist.Domain;

/// <summary>
/// When reservations end and when their reservers are reminded. A monthly job
/// sends the emails, so every end date falls on one of its runs: that way the
/// reminder always comes a full month ahead, and the "ran out" email goes out
/// at the moment the item is freed.
/// </summary>
public static class ReservationSchedule
{
    // Keep in step with the job's cron "0 7 1 * *" in infra/modules/containerapp.bicep.
    public const int RunDayOfMonth = 1;
    public const int RunHourUtc = 7;

    public const int LifetimeMonths = 6;
    public const int ReminderLeadMonths = 1;

    /// <summary>The first job run at or after <paramref name="utc"/>.</summary>
    public static DateTime NextRunOnOrAfter(DateTime utc)
    {
        var run = new DateTime(utc.Year, utc.Month, RunDayOfMonth, RunHourUtc, 0, 0, DateTimeKind.Utc);
        return run >= utc ? run : run.AddMonths(1);
    }

    /// <summary>When a reservation made (or kept) at <paramref name="startUtc"/> ends.</summary>
    public static DateTime EndDateFrom(DateTime startUtc) =>
        NextRunOnOrAfter(startUtc.AddMonths(LifetimeMonths));

    /// <summary>When the reminder for a reservation ending at <paramref name="expiresAt"/> is due.</summary>
    public static DateTime ReminderDueFor(DateTime expiresAt) => expiresAt.AddMonths(-ReminderLeadMonths);
}
