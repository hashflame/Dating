using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blizka.Data.Migrations
{
    /// <inheritdoc />
    public partial class T9_2_InterestIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Interests_NameBe",
                table: "Interests",
                column: "NameBe")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Interests_NameEn",
                table: "Interests",
                column: "NameEn")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Interests_NameRu",
                table: "Interests",
                column: "NameRu")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Interests_NameRu_Unique",
                table: "Interests",
                column: "NameRu",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Interests_NameBe",
                table: "Interests");

            migrationBuilder.DropIndex(
                name: "IX_Interests_NameEn",
                table: "Interests");

            migrationBuilder.DropIndex(
                name: "IX_Interests_NameRu",
                table: "Interests");

            migrationBuilder.DropIndex(
                name: "IX_Interests_NameRu_Unique",
                table: "Interests");
        }
    }
}
