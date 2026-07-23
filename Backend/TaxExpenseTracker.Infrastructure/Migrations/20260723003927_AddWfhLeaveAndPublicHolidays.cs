using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaxExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWfhLeaveAndPublicHolidays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaveEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LeaveDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EntryType = table.Column<int>(type: "INTEGER", nullable: false),
                    HoursWorked = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicHolidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HolidayDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    IsImported = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkFromHomeEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EntryType = table.Column<int>(type: "INTEGER", nullable: false),
                    HoursWorked = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkFromHomeEntries", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PublicHolidays",
                columns: new[] { "Id", "CreatedAt", "HolidayDate", "IsImported", "Name", "Source" },
                values: new object[,]
                {
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "New Year's Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "New Year's Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Australia Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 1, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Australia Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Good Friday", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 3, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Good Friday", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Easter Saturday", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 3, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Easter Saturday", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165009"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Easter Sunday", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 3, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Easter Sunday", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165011"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 6, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Easter Monday", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165012"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 3, 29, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Easter Monday", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165013"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Anzac Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165014"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 4, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Anzac Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165015"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Additional Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165016"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 4, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Additional Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165017"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "King's Birthday", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165018"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 6, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "King's Birthday", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165019"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Bank Holiday", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165020"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 8, 2, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Bank Holiday", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165021"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Labour Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165022"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 10, 4, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Labour Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165023"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Christmas Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165024"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 12, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Christmas Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165025"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 12, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Additional Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165026"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Boxing Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165027"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 12, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Boxing Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165028"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Additional Day", "Seed" },
                    { new Guid("90e87f20-6fd4-4f68-92a8-61fb6f165029"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 12, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), false, "Additional Day", "Seed" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_HolidayDate_Name",
                table: "PublicHolidays",
                columns: new[] { "HolidayDate", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveEntries");

            migrationBuilder.DropTable(
                name: "PublicHolidays");

            migrationBuilder.DropTable(
                name: "WorkFromHomeEntries");
        }
    }
}
