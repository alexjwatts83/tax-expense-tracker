using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkLocationToWorkFromHome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkLocation",
                table: "WorkFromHomeEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkLocation",
                table: "WorkFromHomeEntries");
        }
    }
}
