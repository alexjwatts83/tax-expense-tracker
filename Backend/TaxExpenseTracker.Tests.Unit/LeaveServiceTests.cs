using TaxExpenseTracker.Application.Leave;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.PublicHolidays;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class LeaveServiceTests
{
    [Fact]
    public async Task CreateAsync_UsesFullDayHours_WhenEntryTypeFullDay()
    {
        var repository = new InMemoryLeaveRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new LeaveService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.CreateAsync(new CreateLeaveCommand(new DateTime(2026, 3, 10), DayEntryType.FullDay, null, "  Annual leave  "));

        Assert.Equal(7.6m, result.HoursWorked);
        Assert.Equal("Annual leave", result.Notes);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task CreateAsync_UsesHalfDayHours_WhenEntryTypeHalfDay()
    {
        var repository = new InMemoryLeaveRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new LeaveService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.CreateAsync(new CreateLeaveCommand(new DateTime(2026, 3, 11), DayEntryType.HalfDay, null, null));

        Assert.Equal(3.8m, result.HoursWorked);
    }

    [Fact]
    public async Task CreateAsync_UsesSpecificHours_WhenEntryTypeSpecificHours()
    {
        var repository = new InMemoryLeaveRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new LeaveService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.CreateAsync(new CreateLeaveCommand(new DateTime(2026, 3, 12), DayEntryType.SpecificHours, 6.25m, null));

        Assert.Equal(6.25m, result.HoursWorked);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenDateAlreadyExists()
    {
        var repository = new InMemoryLeaveRepository();
        repository.Entries.Add(LeaveEntry.Create(new DateTime(2026, 3, 12), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new LeaveService(repository, holidayRepository, TestTime.TimeProvider);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new CreateLeaveCommand(new DateTime(2026, 3, 12), DayEntryType.HalfDay, null, null)));

        Assert.Equal("A leave entry already exists for this date.", ex.Message);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsMatchingEntries()
    {
        var repository = new InMemoryLeaveRepository();
        repository.Entries.Add(LeaveEntry.Create(new DateTime(2026, 3, 1), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        repository.Entries.Add(LeaveEntry.Create(new DateTime(2026, 3, 20), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        repository.Entries.Add(LeaveEntry.Create(new DateTime(2026, 4, 1), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        var holidayRepository = new InMemoryPublicHolidayRepository();

        var service = new LeaveService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.GetByDateRangeAsync(new DateTime(2026, 3, 10), new DateTime(2026, 3, 31));

        Assert.Single(result);
        Assert.Equal(new DateTime(2026, 3, 20), result[0].LeaveDate);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenEntryMissing()
    {
        var repository = new InMemoryLeaveRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new LeaveService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task RestoreAsync_RestoresSoftDeletedEntry()
    {
        var repository = new InMemoryLeaveRepository();
        var entry = LeaveEntry.Create(new DateTime(2026, 4, 1), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        entry.SoftDelete(TestTime.TimeProvider);
        repository.Entries.Add(entry);
        var holidayRepository = new InMemoryPublicHolidayRepository();

        var service = new LeaveService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.RestoreAsync(entry.Id);

        Assert.True(result);
        Assert.False(entry.IsDeleted);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenSpecificHoursMissingForSpecificHoursType()
    {
        var repository = new InMemoryLeaveRepository();
        var entry = LeaveEntry.Create(new DateTime(2026, 4, 2), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        repository.Entries.Add(entry);
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new LeaveService(repository, holidayRepository, TestTime.TimeProvider);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateAsync(entry.Id, new UpdateLeaveCommand(new DateTime(2026, 4, 3), DayEntryType.SpecificHours, null, null)));
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenAnotherEntryExistsForDate()
    {
        var repository = new InMemoryLeaveRepository();
        var entry = LeaveEntry.Create(new DateTime(2026, 4, 2), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        var other = LeaveEntry.Create(new DateTime(2026, 4, 3), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        repository.Entries.Add(entry);
        repository.Entries.Add(other);
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new LeaveService(repository, holidayRepository, TestTime.TimeProvider);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(entry.Id, new UpdateLeaveCommand(new DateTime(2026, 4, 3), DayEntryType.FullDay, null, null)));

        Assert.Equal("A leave entry already exists for this date.", ex.Message);
    }

    [Fact]
    public async Task GetSummaryAsync_Month_ReturnsTotalsWithinMonthWindow()
    {
        var repository = new InMemoryLeaveRepository();
        repository.Entries.Add(LeaveEntry.Create(new DateTime(2026, 3, 3), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        repository.Entries.Add(LeaveEntry.Create(new DateTime(2026, 3, 20), DayEntryType.SpecificHours, 2.5m, null, TestTime.TimeProvider));
        repository.Entries.Add(LeaveEntry.Create(new DateTime(2026, 4, 1), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        var holidayRepository = new InMemoryPublicHolidayRepository();
        holidayRepository.Holidays.Add(PublicHoliday.Create(new DateTime(2026, 3, 10), "Labour Day", "Seed", false, TestTime.TimeProvider));
        holidayRepository.Holidays.Add(PublicHoliday.Create(new DateTime(2026, 3, 30), "Regional Holiday", "Seed", false, TestTime.TimeProvider));
        holidayRepository.Holidays.Add(PublicHoliday.Create(new DateTime(2026, 4, 2), "Outside Range", "Seed", false, TestTime.TimeProvider));

        var service = new LeaveService(repository, holidayRepository, TestTime.TimeProvider);

        var summary = await service.GetSummaryAsync(SummaryView.Month, new DateTime(2026, 3, 8));

        Assert.Equal(new DateTime(2026, 3, 1), summary.FromDate);
        Assert.Equal(new DateTime(2026, 3, 31), summary.ToDate);
        Assert.Equal(10.1m, summary.TotalHours);
        Assert.Equal(2, summary.TotalDays);
        Assert.Equal(2, summary.EntryCount);
        Assert.Equal(2, summary.Holidays.Count);
        Assert.Equal(new DateTime(2026, 3, 10), summary.Holidays[0].Date);
        Assert.Equal("Regional Holiday", summary.Holidays[1].Name);
    }

    private sealed class InMemoryPublicHolidayRepository : IPublicHolidayRepository
    {
        public List<PublicHoliday> Holidays { get; } = [];

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

    private sealed class InMemoryLeaveRepository : ILeaveRepository
    {
        public List<LeaveEntry> Entries { get; } = [];
        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<LeaveEntry>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<LeaveEntry>>(Entries.ToList());
        }

        public Task<LeaveEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Entries.FirstOrDefault(x => x.Id == id && !x.IsDeleted));
        }

        public Task<IReadOnlyList<LeaveEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
        {
            var query = Entries.Where(x => !x.IsDeleted);

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.LeaveDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.LeaveDate <= toDate.Value.Date);
            }

            return Task.FromResult<IReadOnlyList<LeaveEntry>>(query.ToList());
        }

        public Task<bool> ExistsForDateAsync(DateTime leaveDate, Guid? excludingId = null, CancellationToken cancellationToken = default)
        {
            var date = leaveDate.Date;
            var exists = Entries.Any(x => !x.IsDeleted && x.LeaveDate.Date == date && (!excludingId.HasValue || x.Id != excludingId.Value));
            return Task.FromResult(exists);
        }

        public Task<LeaveEntry?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Entries.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(LeaveEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}