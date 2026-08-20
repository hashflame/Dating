using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blizka.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFilterAndHasChildren : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasChildren",
                table: "Users",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserFilters",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShowGender = table.Column<string>(type: "text", nullable: false),
                    AgeMin = table.Column<int>(type: "integer", nullable: false),
                    AgeMax = table.Column<int>(type: "integer", nullable: false),
                    MaxDistanceKm = table.Column<int>(type: "integer", nullable: false),
                    DatingGoals = table.Column<string[]>(type: "text[]", nullable: false),
                    RequireFilledProfile = table.Column<bool>(type: "boolean", nullable: false),
                    ActiveWithinDays = table.Column<int>(type: "integer", nullable: true),
                    RequirePhoto = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedOnly = table.Column<bool>(type: "boolean", nullable: false),
                    NonSmoker = table.Column<bool>(type: "boolean", nullable: false),
                    NonDrinker = table.Column<bool>(type: "boolean", nullable: false),
                    NoChildren = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFilters", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserFilters_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserFilters");

            migrationBuilder.DropColumn(
                name: "HasChildren",
                table: "Users");
        }
    }
}
