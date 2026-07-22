using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaxExpenseTracker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeBankEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Banks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banks", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Banks",
                columns: new[] { "Id", "CreatedAt", "IsDeleted", "Name" },
                values: new object[,]
                {
                    { new Guid("4c52ddd3-5208-4385-bf85-c1d3e0402ef4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "CBA" },
                    { new Guid("69c4e618-d714-4429-b5a2-3a35eb50b343"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Westpac" },
                    { new Guid("f2c328b0-6d89-4b66-8ef4-fcbe9970a1fd"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "ANZ" }
                });

            migrationBuilder.AddColumn<Guid>(
                name: "BankIdTmp",
                table: "TaxExpenses",
                type: "TEXT",
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO Banks (Id, Name, IsDeleted, CreatedAt)
                SELECT
                    lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || substr(hex(randomblob(2)), 2) || '-' || substr('89ab', abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)), 2) || '-' || hex(randomblob(6))),
                    trim(t.Bank),
                    0,
                    '2026-01-01T00:00:00.0000000Z'
                FROM TaxExpenses t
                WHERE trim(ifnull(t.Bank, '')) <> ''
                  AND NOT EXISTS (
                      SELECT 1
                      FROM Banks b
                      WHERE lower(trim(b.Name)) = lower(trim(t.Bank))
                  )
                GROUP BY lower(trim(t.Bank));
                """);

            migrationBuilder.Sql("""
                UPDATE TaxExpenses
                SET BankIdTmp = (
                    SELECT b.Id
                    FROM Banks b
                    WHERE lower(trim(b.Name)) = lower(trim(TaxExpenses.Bank))
                    LIMIT 1
                );
                """);

            migrationBuilder.Sql("""
                UPDATE TaxExpenses
                SET BankIdTmp = 'f2c328b0-6d89-4b66-8ef4-fcbe9970a1fd'
                WHERE BankIdTmp IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "Bank",
                table: "TaxExpenses");

            migrationBuilder.RenameColumn(
                name: "BankIdTmp",
                table: "TaxExpenses",
                newName: "BankId");

            migrationBuilder.AlterColumn<Guid>(
                name: "BankId",
                table: "TaxExpenses",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxExpenses_BankId",
                table: "TaxExpenses",
                column: "BankId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxExpenses_Banks_BankId",
                table: "TaxExpenses",
                column: "BankId",
                principalTable: "Banks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxExpenses_Banks_BankId",
                table: "TaxExpenses");

            migrationBuilder.DropIndex(
                name: "IX_TaxExpenses_BankId",
                table: "TaxExpenses");

            migrationBuilder.AddColumn<string>(
                name: "Bank",
                table: "TaxExpenses",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE TaxExpenses
                SET Bank = ifnull((
                    SELECT b.Name
                    FROM Banks b
                    WHERE b.Id = TaxExpenses.BankId
                    LIMIT 1
                ), '');
                """);

            migrationBuilder.DropColumn(
                name: "BankId",
                table: "TaxExpenses");

            migrationBuilder.DropTable(
                name: "Banks");
        }
    }
}
