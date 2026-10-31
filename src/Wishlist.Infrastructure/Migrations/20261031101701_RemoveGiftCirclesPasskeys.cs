using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wishlist.Infrastructure.Migrations
{
    /// <summary>
    /// The app moved from giftcircles.app to kadoozle.app. Passkeys are bound to
    /// the old domain and can't sign in any more, so they are removed: the
    /// security page stops listing them and the "add a passkey" prompt shows again.
    /// Accounts without a password recover through the email link.
    /// </summary>
    public partial class RemoveGiftCirclesPasskeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DELETE FROM "AspNetUserPasskeys";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deleted passkeys can't be restored.
        }
    }
}
