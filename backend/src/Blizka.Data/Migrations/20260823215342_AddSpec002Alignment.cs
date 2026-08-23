using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Blizka.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSpec002Alignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BanReason",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BannedUntil",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelegramUsername",
                table: "Users",
                type: "text",
                nullable: true);

            // Дефолт true — не для новых строк (RecordUserConsentCommandHandler всегда передаёт реальное
            // значение явно), а для бэкафилла уже существующих согласий, принятых до появления этого поля
            // (spec 002, B4/Domain Model): они уже были даны, когда AgeConfirmed нигде не проверялся.
            migrationBuilder.AddColumn<bool>(
                name: "AgeConfirmed",
                table: "UserConsents",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Cities",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Cities",
                type: "text",
                nullable: false,
                defaultValue: "City");

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000001"),
                columns: new[] { "Region", "Type" },
                values: new object[] { null, "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000002"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Гомельская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000003"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Могилёвская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000004"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Витебская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000005"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Гродненская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000006"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Брестская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000007"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Могилёвская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000008"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Брестская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000009"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Минская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000010"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Брестская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000011"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Витебская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000012"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Гомельская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000013"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Минская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000014"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Витебская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000015"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Гродненская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000016"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Минская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000017"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Витебская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000018"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Гомельская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000019"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Гомельская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000020"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Гомельская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000021"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Минская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000022"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Брестская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000023"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Гродненская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000024"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Гродненская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000025"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Минская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000026"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Гродненская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000027"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Минская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000028"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Минская область", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000029"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Польша", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000030"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Польша", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000031"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Польша", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000032"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Польша", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000033"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Польша", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000034"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Литва", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000035"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Литва", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000036"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Латвия", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000037"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Россия", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000038"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Россия", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000039"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Россия", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000040"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Украина", "City" });

            migrationBuilder.UpdateData(
                table: "Cities",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000041"),
                columns: new[] { "Region", "Type" },
                values: new object[] { "Украина", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_Swipes_FromUserId_CreatedAt",
                table: "Swipes",
                columns: new[] { "FromUserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Swipes_FromUserId_CreatedAt",
                table: "Swipes");

            migrationBuilder.DropColumn(
                name: "BanReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BannedUntil",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TelegramUsername",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AgeConfirmed",
                table: "UserConsents");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Cities");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Cities");
        }
    }
}
