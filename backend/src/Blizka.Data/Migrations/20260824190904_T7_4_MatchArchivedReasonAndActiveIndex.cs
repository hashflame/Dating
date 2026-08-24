using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blizka.Data.Migrations
{
    /// <inheritdoc />
    public partial class T7_4_MatchArchivedReasonAndActiveIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchivedReason",
                table: "Matches",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_Status",
                table: "Matches",
                column: "Status",
                filter: "\"Status\" = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Matches_Status",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "ArchivedReason",
                table: "Matches");
        }
    }
}
