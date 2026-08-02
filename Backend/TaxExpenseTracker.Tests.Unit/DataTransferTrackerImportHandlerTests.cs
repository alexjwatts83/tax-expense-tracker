using TaxExpenseTracker.Application.DataTransfer;
using TaxExpenseTracker.Application.Trackers;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class DataTransferTrackerImportHandlerTests
{
    [Theory]
    [InlineData(DataTransferImportMode.InsertOnly)]
    [InlineData(DataTransferImportMode.Upsert)]
    [InlineData(DataTransferImportMode.Replace)]
    public async Task ImportAsync_MissingTracker_CreatesInEveryMode(DataTransferImportMode mode)
    {
        var repository = new TrackerRepository();
        var handler = new DataTransferTrackerImportHandler(repository, TestTime.TimeProvider);
        var trackerId = Guid.NewGuid();

        var result = await handler.ImportAsync(
            [new ReferenceTrackerImportItemDto(trackerId, "Work", "Expenses")],
            new DataTransferImportOptions(mode));

        var tracker = Assert.Single(repository.Trackers);
        Assert.Equal(trackerId, tracker.Id);
        Assert.Equal("Work", tracker.Name);
        Assert.Equal(1, result.CreatedCount);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ImportAsync_Upsert_UpdatesAndRestoresExistingTracker()
    {
        var repository = new TrackerRepository();
        var existing = Tracker.Create("Old", null, TestTime.TimeProvider);
        existing.SoftDelete(TestTime.TimeProvider);
        repository.Trackers.Add(existing);
        var handler = new DataTransferTrackerImportHandler(repository, TestTime.TimeProvider);

        var result = await handler.ImportAsync(
            [new ReferenceTrackerImportItemDto(existing.Id, "Updated", "Description")],
            new DataTransferImportOptions(DataTransferImportMode.Upsert));

        Assert.Equal("Updated", existing.Name);
        Assert.Equal("Description", existing.Description);
        Assert.False(existing.IsDeleted);
        Assert.Equal(1, result.UpdatedCount);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ImportAsync_InsertOnly_SkipsExistingTrackerWithoutMutation()
    {
        var repository = new TrackerRepository();
        var existing = Tracker.Create("Original", null, TestTime.TimeProvider);
        repository.Trackers.Add(existing);
        var handler = new DataTransferTrackerImportHandler(repository, TestTime.TimeProvider);

        var result = await handler.ImportAsync(
            [new ReferenceTrackerImportItemDto(existing.Id, "Updated", "Description")],
            new DataTransferImportOptions(DataTransferImportMode.InsertOnly));

        Assert.Equal("Original", existing.Name);
        Assert.Null(existing.Description);
        Assert.Equal(1, result.SkippedCount);
        Assert.Contains(result.Warnings, x => x.Code == "WARN_INSERT_ONLY_SKIPPED");
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ImportAsync_ReplaceWithDeletes_SoftDeletesTrackerMissingFromPayload()
    {
        var repository = new TrackerRepository();
        var kept = Tracker.Create("Kept", null, TestTime.TimeProvider);
        var missing = Tracker.Create("Missing", null, TestTime.TimeProvider);
        repository.Trackers.AddRange([kept, missing]);
        var handler = new DataTransferTrackerImportHandler(repository, TestTime.TimeProvider);

        var result = await handler.ImportAsync(
            [new ReferenceTrackerImportItemDto(kept.Id, kept.Name, kept.Description)],
            new DataTransferImportOptions(DataTransferImportMode.Replace, AllowDeletes: true));

        Assert.False(kept.IsDeleted);
        Assert.True(missing.IsDeleted);
        Assert.Contains(result.Warnings, x => x.Code == "WARN_REPLACE_SOFT_DELETED_MISSING");
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact]
    public async Task ImportAsync_MultipleTrackers_UsesSingleBulkRead()
    {
        var repository = new TrackerRepository();
        var handler = new DataTransferTrackerImportHandler(repository, TestTime.TimeProvider);

        await handler.ImportAsync(
            [
                new ReferenceTrackerImportItemDto(Guid.NewGuid(), "One", null),
                new ReferenceTrackerImportItemDto(Guid.NewGuid(), "Two", null),
            ],
            new DataTransferImportOptions(DryRun: true));

        Assert.Equal(1, repository.BulkReadCount);
        Assert.Equal(0, repository.SingleReadCount);
    }

    private sealed class TrackerRepository : ITrackerRepository
    {
        public List<Tracker> Trackers { get; } = [];
        public bool SaveChangesCalled { get; private set; }
        public int BulkReadCount { get; private set; }
        public int SingleReadCount { get; private set; }

        public Task<IReadOnlyList<Tracker>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tracker>>(Trackers.Where(x => !x.IsDeleted).ToList());

        public Task<IReadOnlyList<Tracker>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tracker>>(Trackers);

        public Task<Tracker?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Trackers.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

        public Task<Tracker?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            SingleReadCount += 1;
            return Task.FromResult(Trackers.FirstOrDefault(x => x.Id == id));
        }

        public Task<IReadOnlyList<Tracker>> GetAllForUpdateIncludingDeletedAsync(CancellationToken cancellationToken = default)
        {
            BulkReadCount += 1;
            return Task.FromResult<IReadOnlyList<Tracker>>(Trackers);
        }

        public Task<IReadOnlyList<Tracker>> GetByIdsForUpdateIncludingDeletedAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
        {
            BulkReadCount += 1;
            return Task.FromResult<IReadOnlyList<Tracker>>(Trackers.Where(x => ids.Contains(x.Id)).ToList());
        }

        public Task AddAsync(Tracker entity, CancellationToken cancellationToken = default)
        {
            Trackers.Add(entity);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}