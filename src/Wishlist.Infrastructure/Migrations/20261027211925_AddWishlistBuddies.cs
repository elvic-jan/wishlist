using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWishlistBuddies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AddedById",
                table: "WishlistItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WishlistBuddies",
                columns: table => new
                {
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuddyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistBuddies", x => new { x.OwnerId, x.BuddyId });
                    table.ForeignKey(
                        name: "FK_WishlistBuddies_AspNetUsers_BuddyId",
                        column: x => x.BuddyId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WishlistBuddies_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_AddedById",
                table: "WishlistItems",
                column: "AddedById");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistBuddies_BuddyId",
                table: "WishlistBuddies",
                column: "BuddyId");

            migrationBuilder.AddForeignKey(
                name: "FK_WishlistItems_AspNetUsers_AddedById",
                table: "WishlistItems",
                column: "AddedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WishlistItems_AspNetUsers_AddedById",
                table: "WishlistItems");

            migrationBuilder.DropTable(
                name: "WishlistBuddies");

            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_AddedById",
                table: "WishlistItems");

            migrationBuilder.DropColumn(
                name: "AddedById",
                table: "WishlistItems");
        }
    }
}
