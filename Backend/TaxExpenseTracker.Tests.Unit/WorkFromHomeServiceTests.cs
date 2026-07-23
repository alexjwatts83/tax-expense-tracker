using TaxExpenseTracker.Application.WorkFromHome;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class WorkFromHomeServiceTests
{
    [Fact]
    public async Task CreateAsync_UsesFullDayHours_WhenEntryTypeFullDay()
    {
        var repository = new InMemoryWorkFromHomeRepository();
        var service = new WorkFromHomeService(repository);

        var result = await service.CreateAsync(new CreateWorkFromHomeCommand(new DateTime(2026, 1, 10), DayEntryType.FullDay, null, "  Focus  "));

        Assert.Equal(7.6m, result.HoursWorked);
        Assert.Equal("Focus", result.Notes);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task CreateAsync_UsesHalfDayHours_WhenEntryTypeHalfDay()
    {
        var repository = new InMemoryWorkFromHomeRepository();
        var service = new WorkFromHomeService(repository);

        var result = await service.CreateAsync(new CreateWorkFromHomeCommand(new DateTime(2026, 1, 11), DayEntryType.HalfDay, null, null));

        Assert.Equal(3.8m, result.HoursWorked);
    }

    [Fact]
    public async Task CreateAsync_UsesSpecificHours_WhenEntryTypeSpecificHours()
    {
        var repository = new InMemoryWorkFromHomeRepository();
        var service = new WorkFromHomeService(repository);

        var result = await service.CreateAsync(new CreateWorkFromHomeCommand(new DateTime(2026, 1, 12), DayEntryType.SpecificHours, 5.5m, null));

        Assert.Equal(5.5m, result.HoursWorked);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsMatchingEntries()
    {
        var repository = new InMemoryWorkFromHomeRepository();
        repository.Entries.Add(WorkFromHomeEntry.Create(new DateTime(2026, 1, 1), DayEntryType.FullDay, null, null));
        repository.Entries.Add(WorkFromHomeEntry.Create(new DateTime(2026, 1, 15), DayEntryType.FullDay, null, null));
        repository.Entries.Add(WorkFromHomeEntry.Create(new DateTime(2026, 2, 1), DayEntryType.FullDay, null, null));

        var service = new WorkFromHomeService(repository);

        var result = await service.GetByDateRangeAsync(new DateTime(2026, 1, 10), new DateTime(2026, 1, 31));

        Assert.Single(result);
        Assert.Equal(new DateTime(2026, 1, 15), result[0].WorkDate);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenEntryMissing()
    {
        var repository = new InMemoryWorkFromHomeRepository();
        var service = new WorkFromHomeService(repository);

        var result = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task RestoreAsync_RestoresSoftDeletedEntry()
    {
        var repository = new InMemoryWorkFromHomeRepository();
        var entry = WorkFromHomeEntry.Create(new DateTime(2026, 2, 1), DayEntryType.FullDay, null, null);
        entry.SoftDelete();
        repository.Entries.Add(entry);

        var service = new WorkFromHomeService(repository);

        var result = await service.RestoreAsync(entry.Id);

        Assert.True(result);
        Assert.False(entry.IsDeleted);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenSpecificHoursMissingForSpecificHoursType()
    {
        var repository = new InMemoryWorkFromHomeRepository();
        var entry = WorkFromHomeEntry.Create(new DateTime(2026, 2, 2), DayEntryType.FullDay, null, null);
        repository.Entries.Add(entry);
        var service = new WorkFromHomeService(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateAsync(entry.Id, new UpdateWorkFromHomeCommand(new DateTime(2026, 2, 3), DayEntryType.SpecificHours, null, null)));
    }

    [Fact]
    public async Task GetSummaryAsync_Week_ReturnsTotalsWithinMondayToSundayWindow()
    {
        var repository = new InMemoryWorkFromHomeRepository();
        repository.Entries.Add(WorkFromHomeEntry.Create(new DateTime(2026, 1, 12), DayEntryType.FullDay, null, null));
        repository.Entries.Add(WorkFromHomeEntry.Create(new DateTime(2026, 1, 14), DayEntryType.HalfDay, null, null));
        repository.Entries.Add(WorkFromHomeEntry.Create(new DateTime(2026, 1, 19), DayEntryType.FullDay, null, null));

        var service = new WorkFromHomeService(repository);

        var summary = await service.GetSummaryAsync(SummaryView.Week, new DateTime(2026, 1, 14));

        Assert.Equal(new DateTime(2026, 1, 12), summary.FromDate);
        Assert.Equal(new DateTime(2026, 1, 18), summary.ToDate);
        Assert.Equal(11.4m, summary.TotalHours);
        Assert.Equal(2, summary.TotalDays);
        Assert.Equal(2, summary.EntryCount);
    }

    private sealed class InMemoryWorkFromHomeRepository : IWorkFromHomeRepository
    {
        public List<WorkFromHomeEntry> Entries { get; } = [];
        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<WorkFromHomeEntry>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<WorkFromHomeEntry>>(Entries.ToList());
        }

        public Task<WorkFromHomeEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Entries.FirstOrDefault(x => x.Id == id && !x.IsDeleted));
        }

        public Task<IReadOnlyList<WorkFromHomeEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
        {
            var query = Entries.Where(x => !x.IsDeleted);

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.WorkDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.WorkDate <= toDate.Value.Date);
            }

            return Task.FromResult<IReadOnlyList<WorkFromHomeEntry>>(query.ToList());
        }

        public Task<WorkFromHomeEntry?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Entries.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(WorkFromHomeEntry entry, CancellationToken cancellationToken = default)
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