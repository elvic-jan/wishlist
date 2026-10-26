using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyImportAndMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegacyUsers",
                columns: table => new
                {
                    LegacyId = table.Column<int>(type: "integer", nullable: false),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClaimedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClaimedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ImportedItemCount = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyUsers", x => x.LegacyId);
                    table.ForeignKey(
                        name: "FK_LegacyUsers_AspNetUsers_ClaimedByUserId",
                        column: x => x.ClaimedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserMessageStates",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMessageStates", x => new { x.UserId, x.MessageId });
                    table.ForeignKey(
                        name: "FK_UserMessageStates_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegacyItems",
                columns: table => new
                {
                    LegacyId = table.Column<int>(type: "integer", nullable: false),
                    LegacyOwnerId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegacyItems", x => x.LegacyId);
                    table.ForeignKey(
                        name: "FK_LegacyItems_LegacyUsers_LegacyOwnerId",
                        column: x => x.LegacyOwnerId,
                        principalTable: "LegacyUsers",
                        principalColumn: "LegacyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegacyItems_LegacyOwnerId",
                table: "LegacyItems",
                column: "LegacyOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyUsers_ClaimedByUserId",
                table: "LegacyUsers",
                column: "ClaimedByUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyUsers_UserName",
                table: "LegacyUsers",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegacyItems");

            migrationBuilder.DropTable(
                name: "UserMessageStates");

            migrationBuilder.DropTable(
                name: "LegacyUsers");
        }
    }
}
