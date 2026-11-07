using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;

namespace Wishlist.Application.UseCases.Admin;

/// <summary>Totals and growth for the admin page. Months and days are UTC.</summary>
public class GetAdminOverviewUseCase(IAdminQueries queries, TimeProvider time)
{
    public const int MonthsShown = 12;

    public async Task<AdminOverviewDto> ExecuteAsync(CancellationToken ct = default)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var thisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var firstMonth = thisMonth.AddMonths(-(MonthsShown - 1));

        var totals = await queries.GetTotalsAsync(now, ct);
        var signUps = await queries.GetSignUpTimesSinceAsync(firstMonth, ct);
        var byMonth = Enumerable.Range(0, MonthsShown)
            .Select(i => firstMonth.AddMonths(i))
            .Select(m => new MonthlySignUpsDto(m.Year, m.Month, signUps.Count(t => t.Year == m.Year && t.Month == m.Month)))
            .ToList();

        var trackingSince = await queries.GetTrackingSinceAsync(ct);
        var active7 = await ActiveAsync(7, today, trackingSince, ct);
        var active30 = await ActiveAsync(30, today, trackingSince, ct);

        return new AdminOverviewDto(totals, byMonth[^1].Count, byMonth[^2].Count, byMonth, active7, active30, trackingSince);
    }

    private async Task<ActiveUsersDto> ActiveAsync(int days, DateOnly today, DateOnly? trackingSince, CancellationToken ct)
    {
        var currentFrom = today.AddDays(-(days - 1));
        var previousFrom = currentFrom.AddDays(-days);
        var current = await queries.CountActiveUsersAsync(currentFrom, today, ct);
        // Comparing with a period that tracking only partly covered would show a false rise.
        int? previous = trackingSince is { } since && since <= previousFrom
            ? await queries.CountActiveUsersAsync(previousFrom, currentFrom.AddDays(-1), ct)
            : null;
        return new ActiveUsersDto(days, current, previous);
    }
}

public class ListAdminUsersUseCase(IAdminQueries queries)
{
    public Task<IReadOnlyList<AdminUserDto>> ExecuteAsync(CancellationToken ct = default) => queries.ListUsersAsync(ct);
}
