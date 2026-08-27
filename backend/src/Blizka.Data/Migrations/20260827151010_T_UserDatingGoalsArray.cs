using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blizka.Data.Migrations
{
    /// <inheritdoc />
    public partial class T_UserDatingGoalsArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Users.DatingGoal (одиночное поле) → Users.DatingGoals (text[], до двух — как в макете S-04).
            // Раньше в анкету попадала только первая выбранная на онбординге цель, а вторая молча терялась
            // (тикет ClickUp) — колонка меняется через ADD+UPDATE+DROP, а не через голый Drop/Add, чтобы уже
            // сохранённое значение не потерялось для существующих строк.
            migrationBuilder.AddColumn<string[]>(
                name: "DatingGoals",
                table: "Users",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.Sql(
                """
                UPDATE "Users" SET "DatingGoals" = ARRAY["DatingGoal"] WHERE "DatingGoal" IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "DatingGoal",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DatingGoal",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Users" SET "DatingGoal" = "DatingGoals"[1] WHERE array_length("DatingGoals", 1) > 0;
                """);

            migrationBuilder.DropColumn(
                name: "DatingGoals",
                table: "Users");
        }
    }
}
