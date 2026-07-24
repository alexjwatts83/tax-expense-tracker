using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaxExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDefaultTagPalette : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Tags
                SET Color = '#6D5BD0'
                WHERE lower(Name) = 'course';
                """);

            migrationBuilder.Sql("""
                UPDATE Tags
                SET Color = '#2F6FDE'
                WHERE lower(Name) = 'professional';
                """);

            migrationBuilder.Sql("""
                UPDATE Tags
                SET Color = '#2E8B57'
                WHERE lower(Name) = 'tax';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Tags
                SET Color = '#CBD5E1'
                WHERE lower(Name) IN ('course', 'professional', 'tax');
                """);
        }
    }
}
