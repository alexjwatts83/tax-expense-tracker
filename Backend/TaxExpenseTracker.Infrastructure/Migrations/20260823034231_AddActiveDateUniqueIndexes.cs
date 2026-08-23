using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveDateUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalize any legacy non-midnight values to their calendar day so DateOnly reads succeed.
            migrationBuilder.Sql("UPDATE \"WorkLocationEntries\" SET \"WorkDate\" = substr(\"WorkDate\", 1, 10) WHERE length(\"WorkDate\") > 10;");
            migrationBuilder.Sql("UPDATE \"LeaveEntries\" SET \"LeaveDate\" = substr(\"LeaveDate\", 1, 10) WHERE length(\"LeaveDate\") > 10;");
            migrationBuilder.Sql("UPDATE \"TaxExpenses\" SET \"Date\" = substr(\"Date\", 1, 10) WHERE length(\"Date\") > 10;");
            migrationBuilder.Sql("UPDATE \"PublicHolidays\" SET \"HolidayDate\" = substr(\"HolidayDate\", 1, 10) WHERE length(\"HolidayDate\") > 10;");

            // Duplicate preflight/repair: keep the most recently updated active entry per date, soft-delete the rest.
            migrationBuilder.Sql(
                "UPDATE \"WorkLocationEntries\" SET \"IsDeleted\" = 1 WHERE \"IsDeleted\" = 0 AND \"Id\" NOT IN (" +
                "SELECT \"Id\" FROM (SELECT \"Id\", ROW_NUMBER() OVER (PARTITION BY \"WorkDate\" ORDER BY \"UpdatedAt\" DESC, \"Id\" DESC) AS rn " +
                "FROM \"WorkLocationEntries\" WHERE \"IsDeleted\" = 0) WHERE rn = 1);");
            migrationBuilder.Sql(
                "UPDATE \"LeaveEntries\" SET \"IsDeleted\" = 1 WHERE \"IsDeleted\" = 0 AND \"Id\" NOT IN (" +
                "SELECT \"Id\" FROM (SELECT \"Id\", ROW_NUMBER() OVER (PARTITION BY \"LeaveDate\" ORDER BY \"UpdatedAt\" DESC, \"Id\" DESC) AS rn " +
                "FROM \"LeaveEntries\" WHERE \"IsDeleted\" = 0) WHERE rn = 1);");

            migrationBuilder.CreateIndex(
                name: "IX_WorkLocationEntries_WorkDate",
                table: "WorkLocationEntries",
                column: "WorkDate",
                unique: true,
                filter: "\"IsDeleted\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveEntries_LeaveDate",
                table: "LeaveEntries",
                column: "LeaveDate",
                unique: true,
                filter: "\"IsDeleted\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkLocationEntries_WorkDate",
                table: "WorkLocationEntries");

            migrationBuilder.DropIndex(
                name: "IX_LeaveEntries_LeaveDate",
                table: "LeaveEntries");
        }
    }
}
