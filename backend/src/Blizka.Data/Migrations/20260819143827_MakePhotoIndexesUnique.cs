using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blizka.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakePhotoIndexesUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_UserId_SortOrder",
                table: "Photos");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_UserId_IsMain",
                table: "Photos",
                column: "UserId",
                unique: true,
                filter: "\"IsMain\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_UserId_SortOrder",
                table: "Photos",
                columns: new[] { "UserId", "SortOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_UserId_IsMain",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_UserId_SortOrder",
                table: "Photos");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_UserId_SortOrder",
                table: "Photos",
                columns: new[] { "UserId", "SortOrder" });
        }
    }
}
