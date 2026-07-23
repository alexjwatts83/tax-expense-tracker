using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameWorkFromHomeEntriesToWorkLocationEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkFromHomeEntries",
                table: "WorkFromHomeEntries");

            migrationBuilder.RenameTable(
                name: "WorkFromHomeEntries",
                newName: "WorkLocationEntries");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkLocationEntries",
                table: "WorkLocationEntries",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_WorkLocationEntries",
                table: "WorkLocationEntries");

            migrationBuilder.RenameTable(
                name: "WorkLocationEntries",
                newName: "WorkFromHomeEntries");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorkFromHomeEntries",
                table: "WorkFromHomeEntries",
                column: "Id");
        }
    }
}
