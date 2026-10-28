using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Infrastructure.Migrations
{
    /// <summary>
    /// Refresh tokens are stored as SHA-256 hashes. Existing tokens are hashed in
    /// place, so nobody is signed out.
    /// </summary>
    public partial class HashRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens");

            migrationBuilder.RenameColumn(
                name: "Token",
                table: "RefreshTokens",
                newName: "TokenHash");

            // Same hash as TokenService.Hash: SHA-256 of the UTF-8 token, lowercase hex.
            migrationBuilder.Sql(
                """UPDATE "RefreshTokens" SET "TokenHash" = encode(sha256(convert_to("TokenHash", 'UTF8')), 'hex');""");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "RefreshTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens");

            // Hashes can't be turned back into tokens: everyone signs in again.
            migrationBuilder.Sql("""DELETE FROM "RefreshTokens";""");

            migrationBuilder.AlterColumn<string>(
                name: "TokenHash",
                table: "RefreshTokens",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.RenameColumn(
                name: "TokenHash",
                table: "RefreshTokens",
                newName: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);
        }
    }
}
