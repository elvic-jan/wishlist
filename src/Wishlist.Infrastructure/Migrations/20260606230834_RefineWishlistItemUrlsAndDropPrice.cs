using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefineWishlistItemUrlsAndDropPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "WishlistItems");

            migrationBuilder.DropColumn(
                name: "Url",
                table: "WishlistItems");

            migrationBuilder.CreateTable(
                name: "WishlistItemUrls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WishlistItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistItemUrls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WishlistItemUrls_WishlistItems_WishlistItemId",
                        column: x => x.WishlistItemId,
                        principalTable: "WishlistItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItemUrls_WishlistItemId_DisplayOrder",
                table: "WishlistItemUrls",
                columns: new[] { "WishlistItemId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WishlistItemUrls");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "WishlistItems",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "WishlistItems",
                type: "text",
                nullable: true);
        }
    }
}
