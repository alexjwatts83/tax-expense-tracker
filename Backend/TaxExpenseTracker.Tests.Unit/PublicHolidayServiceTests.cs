using TaxExpenseTracker.Application.PublicHolidays;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class PublicHolidayServiceTests
{
    [Fact]
    public async Task ImportAsync_ImportsRows_AndSkipsDuplicates()
    {
        var repository = new InMemoryPublicHolidayRepository();
        repository.Holidays.Add(PublicHoliday.Create(new DateTime(2026, 1, 1), "New Year's Day", "Seed", false, TestTime.TimeProvider));

        var service = new PublicHolidayService(repository, TestTime.TimeProvider);

        var csv = "Date,Name\n2026-01-01,New Year's Day\n2026-01-26,Australia Day\n2026-01-26,Australia Day";

        var result = await service.ImportAsync(csv, "User CSV");

        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.SkippedDuplicateCount);
        Assert.Single(result.Warnings);
        Assert.Contains(repository.Holidays, x => x.Name == "Australia Day" && x.Source == "User CSV");
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ImportAsync_Throws_WhenHeaderMissingRequiredColumns()
    {
        var repository = new InMemoryPublicHolidayRepository();
        var service = new PublicHolidayService(repository, TestTime.TimeProvider);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ImportAsync("When,Title\n2026-01-01,New Year's Day", null));

        Assert.Contains("Missing required Date column", ex.Message);
    }

    [Fact]
    public async Task ImportAsync_Throws_WhenRowDateInvalid()
    {
        var repository = new InMemoryPublicHolidayRepository();
        var service = new PublicHolidayService(repository, TestTime.TimeProvider);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ImportAsync("Date,Name\nnot-a-date,Holiday", null));

        Assert.Contains("Invalid date", ex.Message);
    }

    private sealed class InMemoryPublicHolidayRepository : IPublicHolidayRepository
    {
        public List<PublicHoliday> Holidays { get; } = [];
        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<PublicHoliday>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PublicHoliday>>(Holidays.ToList());
        }

        public Task<PublicHoliday?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Holidays.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(PublicHoliday entity, CancellationToken cancellationToken = default)
        {
            Holidays.Add(entity);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PublicHoliday>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
        {
            var query = Holidays.AsEnumerable();

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.HolidayDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.HolidayDate.Date <= toDate.Value.Date);
            }

            return Task.FromResult<IReadOnlyList<PublicHoliday>>(query.ToList());
        }
    }
}
