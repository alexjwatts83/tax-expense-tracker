using TaxExpenseTracker.Application.WorkLocation;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.PublicHolidays;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class WorkLocationServiceTests
{
    [Fact]
    public async Task CreateAsync_UsesFullDayHours_WhenEntryTypeFullDay()
    {
        var repository = new InMemoryWorkLocationRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.CreateAsync(new CreateWorkLocationCommand(new DateOnly(2026, 1, 10), DayEntryType.FullDay, null, "  Focus  "));

        Assert.Equal(7.6m, result.HoursWorked);
        Assert.Equal("Focus", result.Notes);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task CreateAsync_UsesHalfDayHours_WhenEntryTypeHalfDay()
    {
        var repository = new InMemoryWorkLocationRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.CreateAsync(new CreateWorkLocationCommand(new DateOnly(2026, 1, 11), DayEntryType.HalfDay, null, null));

        Assert.Equal(3.8m, result.HoursWorked);
    }

    [Fact]
    public async Task CreateAsync_UsesSpecificHours_WhenEntryTypeSpecificHours()
    {
        var repository = new InMemoryWorkLocationRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.CreateAsync(new CreateWorkLocationCommand(new DateOnly(2026, 1, 12), DayEntryType.SpecificHours, 5.5m, null));

        Assert.Equal(5.5m, result.HoursWorked);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenDateAlreadyExists()
    {
        var repository = new InMemoryWorkLocationRepository();
        repository.Entries.Add(WorkLocationEntry.Create(new DateOnly(2026, 1, 12), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var ex = await Assert.ThrowsAsync<ApplicationConflictException>(() =>
            service.CreateAsync(new CreateWorkLocationCommand(new DateOnly(2026, 1, 12), DayEntryType.HalfDay, null, null)));

        Assert.Equal("A work-location entry already exists for this date.", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_PersistsOfficeWorkLocation()
    {
        var repository = new InMemoryWorkLocationRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.CreateAsync(new CreateWorkLocationCommand(
            new DateOnly(2026, 1, 13),
            DayEntryType.FullDay,
            null,
            null,
            WorkLocationType.Office));

        Assert.Equal(WorkLocationType.Office, result.WorkLocation);
        Assert.Equal(WorkLocationType.Office, repository.Entries[0].WorkLocation);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsMatchingEntries()
    {
        var repository = new InMemoryWorkLocationRepository();
        repository.Entries.Add(WorkLocationEntry.Create(new DateOnly(2026, 1, 1), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        repository.Entries.Add(WorkLocationEntry.Create(new DateOnly(2026, 1, 15), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        repository.Entries.Add(WorkLocationEntry.Create(new DateOnly(2026, 2, 1), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        var holidayRepository = new InMemoryPublicHolidayRepository();

        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.GetByDateRangeAsync(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 31));

        Assert.Single(result);
        Assert.Equal(new DateOnly(2026, 1, 15), result[0].WorkDate);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenEntryMissing()
    {
        var repository = new InMemoryWorkLocationRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task RestoreAsync_RestoresSoftDeletedEntry()
    {
        var repository = new InMemoryWorkLocationRepository();
        var entry = WorkLocationEntry.Create(new DateOnly(2026, 2, 1), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        entry.SoftDelete(TestTime.TimeProvider);
        repository.Entries.Add(entry);
        var holidayRepository = new InMemoryPublicHolidayRepository();

        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.RestoreAsync(entry.Id);

        Assert.True(result);
        Assert.False(entry.IsDeleted);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task RestoreAsync_Throws_WhenActiveEntryExistsForDate()
    {
        var repository = new InMemoryWorkLocationRepository();
        var deleted = WorkLocationEntry.Create(new DateOnly(2026, 2, 1), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        deleted.SoftDelete(TestTime.TimeProvider);
        var active = WorkLocationEntry.Create(new DateOnly(2026, 2, 1), DayEntryType.HalfDay, null, null, TestTime.TimeProvider);
        repository.Entries.Add(deleted);
        repository.Entries.Add(active);
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var ex = await Assert.ThrowsAsync<ApplicationConflictException>(() => service.RestoreAsync(deleted.Id));

        Assert.Equal("A work-location entry already exists for this date.", ex.Message);
        Assert.True(deleted.IsDeleted);
    }

    [Fact]
    public async Task RestoreAsync_Throws_WhenActiveEntryExistsForSameDateDifferentTime()
    {
        var repository = new InMemoryWorkLocationRepository();
        var deleted = WorkLocationEntry.Create(new DateOnly(2026, 2, 1), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        deleted.SoftDelete(TestTime.TimeProvider);
        var active = WorkLocationEntry.Create(new DateOnly(2026, 2, 1), DayEntryType.HalfDay, null, null, TestTime.TimeProvider);
        repository.Entries.Add(deleted);
        repository.Entries.Add(active);
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var ex = await Assert.ThrowsAsync<ApplicationConflictException>(() => service.RestoreAsync(deleted.Id));

        Assert.Equal("A work-location entry already exists for this date.", ex.Message);
        Assert.True(deleted.IsDeleted);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenSpecificHoursMissingForSpecificHoursType()
    {
        var repository = new InMemoryWorkLocationRepository();
        var entry = WorkLocationEntry.Create(new DateOnly(2026, 2, 2), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        repository.Entries.Add(entry);
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateAsync(entry.Id, new UpdateWorkLocationCommand(new DateOnly(2026, 2, 3), DayEntryType.SpecificHours, null, null)));
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenAnotherEntryExistsForDate()
    {
        var repository = new InMemoryWorkLocationRepository();
        var entry = WorkLocationEntry.Create(new DateOnly(2026, 2, 2), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        var other = WorkLocationEntry.Create(new DateOnly(2026, 2, 3), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        repository.Entries.Add(entry);
        repository.Entries.Add(other);
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var ex = await Assert.ThrowsAsync<ApplicationConflictException>(() =>
            service.UpdateAsync(entry.Id, new UpdateWorkLocationCommand(new DateOnly(2026, 2, 3), DayEntryType.FullDay, null, null)));

        Assert.Equal("A work-location entry already exists for this date.", ex.Message);
    }

    [Fact]
    public async Task GetSummaryAsync_Week_ReturnsTotalsWithinMondayToSundayWindow()
    {
        var repository = new InMemoryWorkLocationRepository();
        repository.Entries.Add(WorkLocationEntry.Create(new DateOnly(2026, 1, 12), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        repository.Entries.Add(WorkLocationEntry.Create(new DateOnly(2026, 1, 14), DayEntryType.HalfDay, null, null, TestTime.TimeProvider));
        repository.Entries.Add(WorkLocationEntry.Create(new DateOnly(2026, 1, 19), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        var holidayRepository = new InMemoryPublicHolidayRepository();
        holidayRepository.Holidays.Add(PublicHoliday.Create(new DateOnly(2026, 1, 12), "Public Holiday A", "Seed", false, TestTime.TimeProvider));
        holidayRepository.Holidays.Add(PublicHoliday.Create(new DateOnly(2026, 1, 18), "Public Holiday B", "Seed", false, TestTime.TimeProvider));
        holidayRepository.Holidays.Add(PublicHoliday.Create(new DateOnly(2026, 1, 20), "Outside Range", "Seed", false, TestTime.TimeProvider));

        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var summary = await service.GetSummaryAsync(SummaryView.Week, new DateOnly(2026, 1, 14));

        Assert.Equal(new DateOnly(2026, 1, 12), summary.FromDate);
        Assert.Equal(new DateOnly(2026, 1, 18), summary.ToDate);
        Assert.Equal(11.4m, summary.TotalHours);
        Assert.Equal(2, summary.TotalDays);
        Assert.Equal(2, summary.EntryCount);
        Assert.Equal(2, summary.Holidays.Count);
        Assert.Equal("Public Holiday A", summary.Holidays[0].Name);
        Assert.Equal(new DateOnly(2026, 1, 18), summary.Holidays[1].Date);
    }

    [Fact]
    public async Task BatchCreateAsync_ReturnsMixedResults_ForCreatedSkippedAndFailedItems()
    {
        var repository = new InMemoryWorkLocationRepository();
        repository.Entries.Add(WorkLocationEntry.Create(new DateOnly(2026, 1, 14), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        var holidayRepository = new InMemoryPublicHolidayRepository();
        holidayRepository.Holidays.Add(PublicHoliday.Create(new DateOnly(2026, 1, 13), "Public Holiday", "Seed", false, TestTime.TimeProvider));

        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.BatchCreateAsync(
        [
            new CreateWorkLocationCommand(new DateOnly(2026, 1, 12), DayEntryType.FullDay, null, null),
            new CreateWorkLocationCommand(new DateOnly(2026, 1, 12), DayEntryType.HalfDay, null, null),
            new CreateWorkLocationCommand(new DateOnly(2026, 1, 13), DayEntryType.FullDay, null, null),
            new CreateWorkLocationCommand(new DateOnly(2026, 1, 14), DayEntryType.FullDay, null, null),
            new CreateWorkLocationCommand(new DateOnly(2026, 1, 15), DayEntryType.SpecificHours, null, null),
        ]);

        Assert.Equal(5, result.TotalRequested);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(2, result.SkippedCount);
        Assert.Equal(2, result.FailedCount);
        Assert.Contains(result.Results, x => x.Status == "Created" && x.WorkDate == new DateOnly(2026, 1, 12));
        Assert.Contains(result.Results, x => x.Status == "FailedConflict" && x.WorkDate == new DateOnly(2026, 1, 13));
        Assert.Contains(result.Results, x => x.Status == "FailedValidation" && x.WorkDate == new DateOnly(2026, 1, 15));
        Assert.Equal(1, repository.GetByDatesCallCount);
        Assert.Equal(0, repository.ExistsForDateCallCount);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task BatchCreateAsync_BoundsRepositoryCalls_WhenBatchSizeGrows()
    {
        var repository = new InMemoryWorkLocationRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);
        var commands = Enumerable.Range(1, 100)
            .Select(day => new CreateWorkLocationCommand(new DateOnly(2026, 1, 1).AddDays(day), DayEntryType.FullDay, null, null))
            .ToList();

        var result = await service.BatchCreateAsync(commands);

        Assert.Equal(100, result.CreatedCount);
        Assert.Equal(1, repository.GetByDatesCallCount);
        Assert.Equal(0, repository.ExistsForDateCallCount);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task GetSummaryAsync_Month_LeapYear_IncludesLeapDayAndRespectsBounds()
    {
        var repository = new InMemoryWorkLocationRepository();
        repository.Entries.Add(WorkLocationEntry.Create(new DateOnly(2028, 2, 28), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        repository.Entries.Add(WorkLocationEntry.Create(new DateOnly(2028, 2, 29), DayEntryType.HalfDay, null, null, TestTime.TimeProvider));
        repository.Entries.Add(WorkLocationEntry.Create(new DateOnly(2028, 3, 1), DayEntryType.FullDay, null, null, TestTime.TimeProvider));
        var holidayRepository = new InMemoryPublicHolidayRepository();
        holidayRepository.Holidays.Add(PublicHoliday.Create(new DateOnly(2028, 2, 29), "Leap Day Holiday", "Seed", false, TestTime.TimeProvider));
        holidayRepository.Holidays.Add(PublicHoliday.Create(new DateOnly(2028, 3, 1), "Outside Range", "Seed", false, TestTime.TimeProvider));

        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var summary = await service.GetSummaryAsync(SummaryView.Month, new DateOnly(2028, 2, 10));

        Assert.Equal(new DateOnly(2028, 2, 1), summary.FromDate);
        Assert.Equal(new DateOnly(2028, 2, 29), summary.ToDate);
        Assert.Equal(11.4m, summary.TotalHours);
        Assert.Equal(2, summary.TotalDays);
        Assert.Equal(2, summary.EntryCount);
        Assert.Single(summary.Holidays);
        Assert.Equal(new DateOnly(2028, 2, 29), summary.Holidays[0].Date);
    }

    [Fact]
    public async Task BatchCreateAsync_TreatsSameDateWithDifferentTimeAsDuplicate()
    {
        var repository = new InMemoryWorkLocationRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.BatchCreateAsync(
        [
            new CreateWorkLocationCommand(new DateOnly(2026, 1, 16), DayEntryType.FullDay, null, null),
            new CreateWorkLocationCommand(new DateOnly(2026, 1, 16), DayEntryType.HalfDay, null, null),
        ]);

        Assert.Equal(2, result.TotalRequested);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Contains(result.Results, x => x.Status == "SkippedDuplicate" && x.WorkDate == new DateOnly(2026, 1, 16));
    }

    [Fact]
    public async Task BatchCreateAsync_UsesHolidayConflictStatus_ForDuplicateHolidayDates()
    {
        var repository = new InMemoryWorkLocationRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        holidayRepository.Holidays.Add(PublicHoliday.Create(new DateOnly(2026, 1, 26), "Public Holiday", "Seed", false, TestTime.TimeProvider));
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.BatchCreateAsync(
        [
            new CreateWorkLocationCommand(new DateOnly(2026, 1, 26), DayEntryType.FullDay, null, null),
            new CreateWorkLocationCommand(new DateOnly(2026, 1, 26), DayEntryType.HalfDay, null, null),
        ]);

        Assert.Equal(2, result.TotalRequested);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(2, result.FailedCount);
        Assert.All(result.Results, x => Assert.Equal("FailedConflict", x.Status));
    }

    [Fact]
    public async Task BatchCreateAsync_AllowsEntries_OnWorkableHoliday()
    {
        var repository = new InMemoryWorkLocationRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        holidayRepository.Holidays.Add(PublicHoliday.Create(new DateOnly(2026, 1, 26), "Optional Work Day", "Seed", false, TestTime.TimeProvider, true));
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.BatchCreateAsync(
        [
            new CreateWorkLocationCommand(new DateOnly(2026, 1, 26), DayEntryType.FullDay, null, null),
        ]);

        Assert.Equal(1, result.TotalRequested);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Contains(result.Results, x => x.Status == "Created" && x.WorkDate == new DateOnly(2026, 1, 26));
    }

    [Fact]
    public async Task BatchCreateAsync_ReturnsEmptyResult_WhenRequestItemsEmpty()
    {
        var repository = new InMemoryWorkLocationRepository();
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.BatchCreateAsync([]);

        Assert.Equal(0, result.TotalRequested);
        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task BatchCreateAsync_CreatesEntry_WhenOnlyExistingEntryForDateIsSoftDeleted()
    {
        var repository = new InMemoryWorkLocationRepository();
        var existing = WorkLocationEntry.Create(new DateOnly(2026, 2, 5), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        existing.SoftDelete(TestTime.TimeProvider);
        repository.Entries.Add(existing);
        var holidayRepository = new InMemoryPublicHolidayRepository();
        var service = new WorkLocationService(repository, holidayRepository, TestTime.TimeProvider);

        var result = await service.BatchCreateAsync(
        [
            new CreateWorkLocationCommand(new DateOnly(2026, 2, 5), DayEntryType.HalfDay, null, null),
        ]);

        Assert.Equal(1, result.TotalRequested);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Contains(result.Results, x => x.Status == "Created" && x.WorkDate == new DateOnly(2026, 2, 5));
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

        public Task RemoveByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
        {
            Holidays.RemoveAll(x => ids.Contains(x.Id));
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PublicHoliday>> GetByDateRangeAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
        {
            var query = Holidays.AsEnumerable();

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.HolidayDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.HolidayDate <= toDate.Value);
            }

            return Task.FromResult<IReadOnlyList<PublicHoliday>>(query.ToList());
        }
    }

    private sealed class InMemoryWorkLocationRepository : IWorkLocationRepository
    {
        public List<WorkLocationEntry> Entries { get; } = [];
        public bool SaveChangesCalled { get; private set; }
        public int GetByDatesCallCount { get; private set; }
        public int ExistsForDateCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }

        public Task<IReadOnlyList<WorkLocationEntry>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<WorkLocationEntry>>(Entries.ToList());
        }

        public Task<IReadOnlyList<WorkLocationEntry>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<WorkLocationEntry>>(Entries.ToList());
        }

        public Task<WorkLocationEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Entries.FirstOrDefault(x => x.Id == id && !x.IsDeleted));
        }

        public Task<IReadOnlyList<WorkLocationEntry>> GetByDateRangeAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
        {
            var query = Entries.Where(x => !x.IsDeleted);

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.WorkDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.WorkDate <= toDate.Value);
            }

            return Task.FromResult<IReadOnlyList<WorkLocationEntry>>(query.ToList());
        }

        public Task<bool> ExistsForDateAsync(DateOnly workDate, Guid? excludingId = null, CancellationToken cancellationToken = default)
        {
            ExistsForDateCallCount += 1;
            var exists = Entries.Any(x => !x.IsDeleted && x.WorkDate == workDate && (!excludingId.HasValue || x.Id != excludingId.Value));
            return Task.FromResult(exists);
        }

        public Task<IReadOnlyList<WorkLocationEntry>> GetByDatesAsync(
            IReadOnlyCollection<DateOnly> workDates,
            CancellationToken cancellationToken = default)
        {
            GetByDatesCallCount += 1;
            var dates = workDates.ToHashSet();
            return Task.FromResult<IReadOnlyList<WorkLocationEntry>>(
                Entries.Where(x => !x.IsDeleted && dates.Contains(x.WorkDate)).ToList());
        }

        public Task<WorkLocationEntry?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Entries.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(WorkLocationEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            SaveChangesCallCount += 1;
            return Task.CompletedTask;
        }
    }
}