using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecretSantaDrawings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "Groups",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                // Every existing group is a permanent circle.
                defaultValue: "Circle");

            migrationBuilder.CreateTable(
                name: "Drawings",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerId = table.Column<Guid>(type: "uuid", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Budget = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DrawnAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drawings", x => x.GroupId);
                    table.ForeignKey(
                        name: "FK_Drawings_AspNetUsers_OrganizerId",
                        column: x => x.OrganizerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Drawings_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrawingParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InviteCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawingParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrawingParticipants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DrawingParticipants_Drawings_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Drawings",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrawingAssignments",
                columns: table => new
                {
                    GiverParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverParticipantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawingAssignments", x => x.GiverParticipantId);
                    table.ForeignKey(
                        name: "FK_DrawingAssignments_DrawingParticipants_GiverParticipantId",
                        column: x => x.GiverParticipantId,
                        principalTable: "DrawingParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrawingAssignments_DrawingParticipants_ReceiverParticipantId",
                        column: x => x.ReceiverParticipantId,
                        principalTable: "DrawingParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrawingAssignments_Drawings_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Drawings",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrawingExclusions",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecondParticipantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawingExclusions", x => new { x.GroupId, x.FirstParticipantId, x.SecondParticipantId });
                    table.ForeignKey(
                        name: "FK_DrawingExclusions_DrawingParticipants_FirstParticipantId",
                        column: x => x.FirstParticipantId,
                        principalTable: "DrawingParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrawingExclusions_DrawingParticipants_SecondParticipantId",
                        column: x => x.SecondParticipantId,
                        principalTable: "DrawingParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DrawingExclusions_Drawings_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Drawings",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrawingAssignments_GroupId",
                table: "DrawingAssignments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_DrawingAssignments_ReceiverParticipantId",
                table: "DrawingAssignments",
                column: "ReceiverParticipantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrawingExclusions_FirstParticipantId",
                table: "DrawingExclusions",
                column: "FirstParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_DrawingExclusions_SecondParticipantId",
                table: "DrawingExclusions",
                column: "SecondParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_DrawingParticipants_GroupId_UserId",
                table: "DrawingParticipants",
                columns: new[] { "GroupId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrawingParticipants_InviteCode",
                table: "DrawingParticipants",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrawingParticipants_UserId",
                table: "DrawingParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Drawings_OrganizerId",
                table: "Drawings",
                column: "OrganizerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrawingAssignments");

            migrationBuilder.DropTable(
                name: "DrawingExclusions");

            migrationBuilder.DropTable(
                name: "DrawingParticipants");

            migrationBuilder.DropTable(
                name: "Drawings");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Groups");
        }
    }
}
