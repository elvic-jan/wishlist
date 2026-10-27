using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCircleInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CircleInvitations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    InviteeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedById = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircleInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CircleInvitations_AspNetUsers_InvitedById",
                        column: x => x.InvitedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CircleInvitations_AspNetUsers_InviteeId",
                        column: x => x.InviteeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CircleInvitations_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CircleInvitations_GroupId_InviteeId",
                table: "CircleInvitations",
                columns: new[] { "GroupId", "InviteeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CircleInvitations_InvitedById",
                table: "CircleInvitations",
                column: "InvitedById");

            migrationBuilder.CreateIndex(
                name: "IX_CircleInvitations_InviteeId",
                table: "CircleInvitations",
                column: "InviteeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CircleInvitations");
        }
    }
}
