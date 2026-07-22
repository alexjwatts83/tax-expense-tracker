using TaxExpenseTracker.Application.Trackers;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class TrackerServiceTests
{
    [Fact]
    public async Task CreateAsync_TrimsNameAndDescription_AndPersists()
    {
        var repository = new InMemoryTrackerRepository();
        var service = new TrackerService(repository);

        var result = await service.CreateAsync(new CreateTrackerCommand("  Home Office  ", "  2026 taxes  "));

        Assert.Equal("Home Office", result.Name);
        Assert.Equal("2026 taxes", result.Description);
        Assert.Single(repository.Trackers);
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
