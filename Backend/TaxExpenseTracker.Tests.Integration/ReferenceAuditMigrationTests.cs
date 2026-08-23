using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TaxExpenseTracker.Infrastructure.Data;

namespace TaxExpenseTracker.Tests.Integration;

public class ReferenceAuditMigrationTests
{
    private const string PreviousMigration = "20260823034231_AddActiveDateUniqueIndexes";

    [Fact]
    public async Task LatestMigration_BackfillsReferenceUpdatedAtFromCreatedAt()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"tax-expense-tracker-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={databasePath};Pooling=False")
            .Options;
        var createdAt = new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        var trackerId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var bankId = Guid.NewGuid();
        var holidayId = Guid.NewGuid();
        const string trackerName = "Tracker";
        const string tagName = "Tag";
        const string tagColor = "#123456";
        const string bankName = "Bank";
        const string holidayName = "Holiday";
        const string holidayDate = "2024-02-04";

        try
        {
            await using (var oldContext = new AppDbContext(options))
            {
                await oldContext.Database.GetService<IMigrator>().MigrateAsync(PreviousMigration);
                await oldContext.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO \"Trackers\" (\"Id\", \"Name\", \"Description\", \"CreatedAt\", \"UpdatedAt\", \"IsDeleted\") VALUES ({trackerId}, {trackerName}, NULL, {createdAt}, {createdAt}, 0)");
                await oldContext.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO \"Tags\" (\"Id\", \"Name\", \"Color\", \"CreatedAt\", \"IsDeleted\") VALUES ({tagId}, {tagName}, {tagColor}, {createdAt}, 0)");
                await oldContext.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO \"Banks\" (\"Id\", \"Name\", \"CreatedAt\", \"IsDeleted\") VALUES ({bankId}, {bankName}, {createdAt}, 0)");
                await oldContext.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO \"PublicHolidays\" (\"Id\", \"HolidayDate\", \"Name\", \"Source\", \"IsImported\", \"CanBeWorkedOn\", \"CreatedAt\") VALUES ({holidayId}, {holidayDate}, {holidayName}, NULL, 0, 0, {createdAt})");
            }

            await using (var upgradedContext = new AppDbContext(options))
            {
                await upgradedContext.Database.MigrateAsync();

                Assert.Equal(createdAt, (await upgradedContext.Tags.IgnoreQueryFilters().SingleAsync(x => x.Id == tagId)).UpdatedAt);
                Assert.Equal(createdAt, (await upgradedContext.Banks.IgnoreQueryFilters().SingleAsync(x => x.Id == bankId)).UpdatedAt);
                Assert.Equal(createdAt, (await upgradedContext.PublicHolidays.SingleAsync(x => x.Id == holidayId)).UpdatedAt);
                Assert.Equal(createdAt, (await upgradedContext.Trackers.IgnoreQueryFilters().SingleAsync(x => x.Id == trackerId)).UpdatedAt);
            }
        }
        finally
        {
            File.Delete(databasePath);
        }
    }
}