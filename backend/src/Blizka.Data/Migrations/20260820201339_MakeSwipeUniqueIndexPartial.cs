using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blizka.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeSwipeUniqueIndexPartial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Swipes_FromUserId_ToUserId",
                table: "Swipes");

            migrationBuilder.CreateIndex(
                name: "IX_Swipes_FromUserId_ToUserId",
                table: "Swipes",
                columns: new[] { "FromUserId", "ToUserId" },
                unique: true,
                filter: "\"UndoneAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Swipes_FromUserId_ToUserId",
                table: "Swipes");

            migrationBuilder.CreateIndex(
                name: "IX_Swipes_FromUserId_ToUserId",
                table: "Swipes",
                columns: new[] { "FromUserId", "ToUserId" },
                unique: true);
        }
    }
}
