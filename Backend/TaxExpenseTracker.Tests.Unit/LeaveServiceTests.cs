using TaxExpenseTracker.Application.Leave;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class LeaveServiceTests
{
    [Fact]
    public async Task CreateAsync_UsesFullDayHours_WhenEntryTypeFullDay()
    {
        var repository = new InMemoryLeaveRepository();
        var service = new LeaveService(repository);

        var result = await service.CreateAsync(new CreateLeaveCommand(new DateTime(2026, 3, 10), DayEntryType.FullDay, null, "  Annual leave  "));

        Assert.Equal(7.6m, result.HoursWorked);
        Assert.Equal("Annual leave", result.Notes);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task CreateAsync_UsesHalfDayHours_WhenEntryTypeHalfDay()
    {
        var repository = new InMemoryLeaveRepository();
        var service = new LeaveService(repository);

        var result = await service.CreateAsync(new CreateLeaveCommand(new DateTime(2026, 3, 11), DayEntryType.HalfDay, null, null));

        Assert.Equal(3.8m, result.HoursWorked);
    }

    [Fact]
    public async Task CreateAsync_UsesSpecificHours_WhenEntryTypeSpecificHours()
    {
        var repository = new InMemoryLeaveRepository();
        var service = new LeaveService(repository);

        var result = await service.CreateAsync(new CreateLeaveCommand(new DateTime(2026, 3, 12), DayEntryType.SpecificHours, 6.25m, null));

        Assert.Equal(6.25m, result.HoursWorked);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsMatchingEntries()
    {
        var repository = new InMemoryLeaveRepository();
        repository.Entries.Add(LeaveEntry.Create(new DateTime(2026, 3, 1), DayEntryType.FullDay, null, null));
        repository.Entries.Add(LeaveEntry.Create(new DateTime(2026, 3, 20), DayEntryType.FullDay, null, null));
        repository.Entries.Add(LeaveEntry.Create(new DateTime(2026, 4, 1), DayEntryType.FullDay, null, null));

        var service = new LeaveService(repository);

        var result = await service.GetByDateRangeAsync(new DateTime(2026, 3, 10), new DateTime(2026, 3, 31));

        Assert.Single(result);
        Assert.Equal(new DateTime(2026, 3, 20), result[0].LeaveDate);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenEntryMissing()
    {
        var repository = new InMemoryLeaveRepository();
        var service = new LeaveService(repository);

        var result = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task RestoreAsync_RestoresSoftDeletedEntry()
    {
        var repository = new InMemoryLeaveRepository();
        var entry = LeaveEntry.Create(new DateTime(2026, 4, 1), DayEntryType.FullDay, null, null);
        entry.SoftDelete();
        repository.Entries.Add(entry);

        var service = new LeaveService(repository);

        var result = await service.RestoreAsync(entry.Id);

        Assert.True(result);
        Assert.False(entry.IsDeleted);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenSpecificHoursMissingForSpecificHoursType()
    {
        var repository = new InMemoryLeaveRepository();
        var entry = LeaveEntry.Create(new DateTime(2026, 4, 2), DayEntryType.FullDay, null, null);
        repository.Entries.Add(entry);
        var service = new LeaveService(repository);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.UpdateAsync(entry.Id, new UpdateLeaveCommand(new DateTime(2026, 4, 3), DayEntryType.SpecificHours, null, null)));
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