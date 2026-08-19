using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blizka.Data.Migrations
{
    /// <summary>
    /// Никакого DDL здесь намеренно нет: <c>xmin</c> — системная колонка, которая уже существует у
    /// каждой таблицы Postgres. Сгенерированный по умолчанию <c>dotnet ef migrations add</c> код пытался
    /// сделать <c>ALTER TABLE "Users" ADD COLUMN "xmin"</c>, что Postgres отклоняет с ошибкой
    /// "column name conflicts with a system column name" — эта миграция правит его руками (Up/Down —
    /// no-op), оставляя только модельные метаданные (Designer.cs/ModelSnapshot), которые говорят EF Core
    /// использовать уже существующую системную колонку как concurrency-токен.
    /// </summary>
    public partial class AddUserXminConcurrencyToken : Migration
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
