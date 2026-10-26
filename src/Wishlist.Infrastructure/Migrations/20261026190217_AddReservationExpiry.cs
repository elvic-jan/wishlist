using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "GiftClaims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                table: "GiftClaims",
                type: "timestamp with time zone",
                nullable: true);

            // Existing reservations end like new ones (ReservationSchedule.EndDateFrom):
            // 6 months after they were made, rounded up to the next monthly run
            // (the 1st at 07:00 UTC). Bought claims never end.
            migrationBuilder.Sql("""
                UPDATE "GiftClaims" AS c
                SET "ExpiresAt" = (CASE WHEN run >= t THEN run ELSE run + interval '1 month' END) AT TIME ZONE 'UTC'
                FROM (
                    SELECT "Id",
                           ("ClaimedAt" AT TIME ZONE 'UTC') + interval '6 months' AS t,
                           date_trunc('month', ("ClaimedAt" AT TIME ZONE 'UTC') + interval '6 months') + interval '7 hours' AS run
                    FROM "GiftClaims"
                ) AS d
                WHERE c."Id" = d."Id" AND c."Status" = 'Reserved';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "GiftClaims");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                table: "GiftClaims");
        }
    }
}
