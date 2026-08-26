using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blizka.Data.Migrations
{
    /// <inheritdoc />
    public partial class T16_1_AddPrivacySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrivacySettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockIncomingMessages = table.Column<bool>(type: "boolean", nullable: false),
                    HideDistance = table.Column<bool>(type: "boolean", nullable: false),
                    HideAge = table.Column<bool>(type: "boolean", nullable: false),
                    ShowLastActive = table.Column<bool>(type: "boolean", nullable: false),
                    InvisibleMode = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacySettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivacySettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrivacySettings_UserId",
                table: "PrivacySettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivacySettings");
        }
    }
}
