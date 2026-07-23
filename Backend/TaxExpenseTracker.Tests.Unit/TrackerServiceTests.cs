using TaxExpenseTracker.Application.Trackers;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class TrackerServiceTests
{
    [Fact]
    public async Task CreateAsync_TrimsNameAndDescription_AndPersists()
    {
        var repository = new InMemoryTrackerRepository();
        var service = new TrackerService(repository, TestTime.TimeProvider);

        var result = await service.CreateAsync(new CreateTrackerCommand("  Home Office  ", "  2026 taxes  "));

        Assert.Equal("Home Office", result.Name);
        Assert.Equal("2026 taxes", result.Description);
        Assert.Single(repository.Trackers);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenTrackerMissing()
    {
        var repository = new InMemoryTrackerRepository();
        var service = new TrackerService(repository, TestTime.TimeProvider);

        var result = await service.UpdateAsync(Guid.NewGuid(), new UpdateTrackerCommand("Name", "Desc"));

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenTrackerMissing()
    {
        var repository = new InMemoryTrackerRepository();
        var service = new TrackerService(repository, TestTime.TimeProvider);

        var result = await service.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task RestoreAsync_RestoresTracker_WhenSoftDeleted()
    {
        var repository = new InMemoryTrackerRepository();
        var tracker = Tracker.Create("Home Office", "Desc", TestTime.TimeProvider);
        tracker.SoftDelete(TestTime.TimeProvider);
        repository.Trackers.Add(tracker);

        var service = new TrackerService(repository, TestTime.TimeProvider);

        var result = await service.RestoreAsync(tracker.Id);

        Assert.True(result);
        Assert.False(tracker.IsDeleted);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTracker_WhenExists()
    {
        var repository = new InMemoryTrackerRepository();
        var tracker = Tracker.Create("Old", "Old desc", TestTime.TimeProvider);
        repository.Trackers.Add(tracker);

        var service = new TrackerService(repository, TestTime.TimeProvider);

        var result = await service.UpdateAsync(tracker.Id, new UpdateTrackerCommand("  New  ", "  New desc  "));

        Assert.True(result);
        Assert.Equal("New", tracker.Name);
        Assert.Equal("New desc", tracker.Description);
        Assert.True(repository.SaveChangesCalled);
    }

    private sealed class InMemoryTrackerRepository : ITrackerRepository
    {
        public List<Tracker> Trackers { get; } = [];
        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<Tracker>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Tracker>>(Trackers.ToList());
        }

        public Task<Tracker?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Trackers.FirstOrDefault(x => x.Id == id));
        }

        public Task<Tracker?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Trackers.FirstOrDefault(x => x.Id == id));
        }

        public Task AddAsync(Tracker tracker, CancellationToken cancellationToken = default)
        {
            Trackers.Add(tracker);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
