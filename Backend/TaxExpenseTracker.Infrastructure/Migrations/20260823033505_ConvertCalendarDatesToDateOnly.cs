using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertCalendarDatesToDateOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165001"),
                column: "HolidayDate",
                value: new DateOnly(2026, 1, 1));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165002"),
                column: "HolidayDate",
                value: new DateOnly(2027, 1, 1));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165003"),
                column: "HolidayDate",
                value: new DateOnly(2026, 1, 26));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165004"),
                column: "HolidayDate",
                value: new DateOnly(2027, 1, 26));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165005"),
                column: "HolidayDate",
                value: new DateOnly(2026, 4, 3));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165006"),
                column: "HolidayDate",
                value: new DateOnly(2027, 3, 26));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165007"),
                column: "HolidayDate",
                value: new DateOnly(2026, 4, 4));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165008"),
                column: "HolidayDate",
                value: new DateOnly(2027, 3, 27));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165009"),
                column: "HolidayDate",
                value: new DateOnly(2026, 4, 5));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165010"),
                column: "HolidayDate",
                value: new DateOnly(2027, 3, 28));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165011"),
                column: "HolidayDate",
                value: new DateOnly(2026, 4, 6));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165012"),
                column: "HolidayDate",
                value: new DateOnly(2027, 3, 29));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165013"),
                column: "HolidayDate",
                value: new DateOnly(2026, 4, 25));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165014"),
                column: "HolidayDate",
                value: new DateOnly(2027, 4, 25));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165015"),
                column: "HolidayDate",
                value: new DateOnly(2026, 4, 27));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165016"),
                column: "HolidayDate",
                value: new DateOnly(2027, 4, 26));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165017"),
                column: "HolidayDate",
                value: new DateOnly(2026, 6, 8));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165018"),
                column: "HolidayDate",
                value: new DateOnly(2027, 6, 14));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165019"),
                column: "HolidayDate",
                value: new DateOnly(2026, 8, 3));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165020"),
                column: "HolidayDate",
                value: new DateOnly(2027, 8, 2));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165021"),
                column: "HolidayDate",
                value: new DateOnly(2026, 10, 5));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165022"),
                column: "HolidayDate",
                value: new DateOnly(2027, 10, 4));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165023"),
                column: "HolidayDate",
                value: new DateOnly(2026, 12, 25));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165024"),
                column: "HolidayDate",
                value: new DateOnly(2027, 12, 25));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165025"),
                column: "HolidayDate",
                value: new DateOnly(2027, 12, 27));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165026"),
                column: "HolidayDate",
                value: new DateOnly(2026, 12, 26));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165027"),
                column: "HolidayDate",
                value: new DateOnly(2027, 12, 26));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165028"),
                column: "HolidayDate",
                value: new DateOnly(2026, 12, 28));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165029"),
                column: "HolidayDate",
                value: new DateOnly(2027, 12, 28));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165001"),
                column: "HolidayDate",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165002"),
                column: "HolidayDate",
                value: new DateTime(2027, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165003"),
                column: "HolidayDate",
                value: new DateTime(2026, 1, 26, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165004"),
                column: "HolidayDate",
                value: new DateTime(2027, 1, 26, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165005"),
                column: "HolidayDate",
                value: new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165006"),
                column: "HolidayDate",
                value: new DateTime(2027, 3, 26, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165007"),
                column: "HolidayDate",
                value: new DateTime(2026, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165008"),
                column: "HolidayDate",
                value: new DateTime(2027, 3, 27, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165009"),
                column: "HolidayDate",
                value: new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165010"),
                column: "HolidayDate",
                value: new DateTime(2027, 3, 28, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165011"),
                column: "HolidayDate",
                value: new DateTime(2026, 4, 6, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165012"),
                column: "HolidayDate",
                value: new DateTime(2027, 3, 29, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165013"),
                column: "HolidayDate",
                value: new DateTime(2026, 4, 25, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165014"),
                column: "HolidayDate",
                value: new DateTime(2027, 4, 25, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165015"),
                column: "HolidayDate",
                value: new DateTime(2026, 4, 27, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165016"),
                column: "HolidayDate",
                value: new DateTime(2027, 4, 26, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165017"),
                column: "HolidayDate",
                value: new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165018"),
                column: "HolidayDate",
                value: new DateTime(2027, 6, 14, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165019"),
                column: "HolidayDate",
                value: new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165020"),
                column: "HolidayDate",
                value: new DateTime(2027, 8, 2, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165021"),
                column: "HolidayDate",
                value: new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165022"),
                column: "HolidayDate",
                value: new DateTime(2027, 10, 4, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165023"),
                column: "HolidayDate",
                value: new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165024"),
                column: "HolidayDate",
                value: new DateTime(2027, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165025"),
                column: "HolidayDate",
                value: new DateTime(2027, 12, 27, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165026"),
                column: "HolidayDate",
                value: new DateTime(2026, 12, 26, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165027"),
                column: "HolidayDate",
                value: new DateTime(2027, 12, 26, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165028"),
                column: "HolidayDate",
                value: new DateTime(2026, 12, 28, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "PublicHolidays",
                keyColumn: "Id",
                keyValue: new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165029"),
                column: "HolidayDate",
                value: new DateTime(2027, 12, 28, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
