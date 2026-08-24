using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blizka.Data.Migrations
{
    /// <summary>
    /// Никакого DDL здесь намеренно нет — тот же случай, что и <see cref="AddUserXminConcurrencyToken"/>:
    /// <c>xmin</c> уже существует как системная колонка Postgres у каждой таблицы. Сгенерированный по
    /// умолчанию <c>dotnet ef migrations add</c> код пытался сделать
    /// <c>ALTER TABLE "Matches" ADD COLUMN "xmin"</c>, что Postgres отклоняет с ошибкой "column name
    /// conflicts with a system column name" — эта миграция правит его руками (Up/Down — no-op), оставляя
    /// только модельные метаданные (Designer.cs/ModelSnapshot), которые говорят EF Core использовать уже
    /// существующую системную колонку как concurrency-токен для <c>Match</c> (T-7.3).
    /// </summary>
    public partial class T7_3_MatchXminConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
