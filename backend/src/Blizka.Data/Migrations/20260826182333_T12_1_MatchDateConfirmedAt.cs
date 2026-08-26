using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blizka.Data.Migrations
{
    /// <inheritdoc />
    public partial class T12_1_MatchDateConfirmedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateConfirmedAt",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DateConfirmedByUserId",
                table: "Matches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_DateConfirmedByUserId",
                table: "Matches",
                column: "DateConfirmedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_Users_DateConfirmedByUserId",
                table: "Matches",
                column: "DateConfirmedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_Users_DateConfirmedByUserId",
                table: "Matches");

            migrationBuilder.DropIndex(
                name: "IX_Matches_DateConfirmedByUserId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "DateConfirmedAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "DateConfirmedByUserId",
                table: "Matches");
        }
    }
}
