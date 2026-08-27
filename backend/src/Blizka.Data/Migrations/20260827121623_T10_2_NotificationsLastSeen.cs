using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blizka.Data.Migrations
{
    /// <inheritdoc />
    public partial class T10_2_NotificationsLastSeen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSeenLikesAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSeenMatchesAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenLikesAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastSeenMatchesAt",
                table: "Users");
        }
    }
}
