using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.Interfaces;
using Wishlist.Domain.Enums;
using Wishlist.Infrastructure.Persistence;

namespace Wishlist.API.Tests;

/// <summary>
/// The reservation-expiry migration fills end dates for existing claims, and the
/// monthly job's queries find the right reservations, against real Postgres.
/// </summary>
[Collection(PostgresCollection.Name)]
public class ReservationPostgresTests
{
    private const string MigrationBeforeExpiry = "20261025180730_AddPasskeys";

    private static DateTime Utc(int y, int m, int d, int h = 0) => new(y, m, d, h, 0, 0, DateTimeKind.Utc);

    [PostgresFact]
    public async Task Migration_and_job_queries_handle_end_dates()
    {
        await using var api = new PostgresApi(Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!);
        await using var scope = api.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<WishlistDbContext>();

        // A database from before this change, with one reservation and one bought gift.
        await db.Database.EnsureDeletedAsync();
        await db.GetService<IMigrator>().MigrateAsync(MigrationBeforeExpiry);

        var users = services.GetRequiredService<IUserRepository>();
        var owner = await users.CreateAsync("owner@example.test", "Owner", "Passw0rd!long");
        var giver = await users.CreateAsync("giver@example.test", "Giver", "Passw0rd!long");
        // Plain SQL: today's repository writes columns this old schema doesn't have yet.
        var reservedItemId = Guid.NewGuid();
        var boughtItemId = Guid.NewGuid();
        var reservedId = Guid.NewGuid();
        var boughtId = Guid.NewGuid();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO "WishlistItems" ("Id", "OwnerId", "Title", "CreatedAt", "UpdatedAt") VALUES
                ({reservedItemId}, {owner.Id}, 'Red bike', {Utc(2027, 1, 1)}, {Utc(2027, 1, 1)}),
                ({boughtItemId}, {owner.Id}, 'Book', {Utc(2027, 1, 1)}, {Utc(2027, 1, 1)})
            """);
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO "GiftClaims" ("Id", "WishlistItemId", "ClaimedByUserId", "Status", "ClaimedAt") VALUES
                ({reservedId}, {reservedItemId}, {giver.Id}, 'Reserved', {Utc(2027, 1, 2, 10)}),
                ({boughtId}, {boughtItemId}, {giver.Id}, 'Bought', {Utc(2027, 1, 2, 10)})
            """);

        await db.Database.MigrateAsync();
        db.ChangeTracker.Clear();

        // Same rule as ReservationSchedule.EndDateFrom: 2 Jan + 6 months, rounded up to 1 Aug 07:00 UTC.
        var reserved = await db.GiftClaims.AsNoTracking().SingleAsync(c => c.Id == reservedId);
        var bought = await db.GiftClaims.AsNoTracking().SingleAsync(c => c.Id == boughtId);
        Assert.Equal(Utc(2027, 8, 1, 7), reserved.ExpiresAt);
        Assert.Null(bought.ExpiresAt);
        Assert.Equal(GiftClaimStatus.Bought, bought.Status);

        var claims = services.GetRequiredService<IGiftClaimRepository>();

        // 1 June: the reservation ends after the next run, so nothing is due.
        Assert.Empty(await claims.GetReminderDueAsync(Utc(2027, 6, 1, 8), Utc(2027, 7, 1, 8)));

        // 1 July: reminder due, with what the email needs.
        var due = Assert.Single(await claims.GetReminderDueAsync(Utc(2027, 7, 1, 8), Utc(2027, 8, 1, 8)));
        Assert.Equal((reservedId, giver.Id, owner.Id, "Red bike"), (due.ClaimId, due.ClaimerId, due.OwnerId, due.ItemTitle));
        await claims.MarkReminderSentAsync(reservedId, Utc(2027, 7, 1, 7));
        Assert.Empty(await claims.GetReminderDueAsync(Utc(2027, 7, 1, 8), Utc(2027, 8, 1, 8)));
        Assert.Empty(await claims.GetExpiredAsync(Utc(2027, 7, 1, 8)));

        // 1 August: expired; the bought gift never is.
        var expired = Assert.Single(await claims.GetExpiredAsync(Utc(2027, 8, 1, 8)));
        Assert.Equal(reservedId, expired.ClaimId);
        Assert.DoesNotContain(await claims.GetExpiredAsync(Utc(2040, 1, 1)), n => n.ClaimId == boughtId);
    }
}
