using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.DataTransfer;
using TaxExpenseTracker.Application.Leave;
using TaxExpenseTracker.Application.WorkLocation;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class DataTransferTransactionalReplaceTests
{
    [Fact]
    public async Task WorkLocationImport_ReplaceDryRun_ReportsMissingWithoutMutation()
    {
        var repository = new WorkLocationRepository();
        var kept = WorkLocationEntry.Create(new DateTime(2026, 7, 1), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        var missing = WorkLocationEntry.Create(new DateTime(2026, 7, 2), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        repository.Entries.AddRange([kept, missing]);
        var handler = new DataTransferWorkLocationImportHandler(repository, TestTime.TimeProvider);

        var result = await handler.ImportAsync(
            new WorkLocationImportPayloadDto(
                [new WorkLocationEntryImportItemDto(kept.Id, kept.WorkDate, kept.EntryType, null, kept.Notes, kept.WorkLocation, null, null, false)]),
            new DataTransferImportOptions(DataTransferImportMode.Replace, DryRun: true, AllowDeletes: true));

        Assert.False(missing.IsDeleted);
        Assert.False(repository.SaveChangesCalled);
        Assert.Contains(result.Warnings, x => x.Code == "WARN_REPLACE_SOFT_DELETED_MISSING");
    }

    [Fact]
    public async Task LeaveImport_ReplaceWithDeletes_SoftDeletesMissingEntry()
    {
        var repository = new LeaveRepository();
        var kept = LeaveEntry.Create(new DateTime(2026, 7, 1), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        var missing = LeaveEntry.Create(new DateTime(2026, 7, 2), DayEntryType.FullDay, null, null, TestTime.TimeProvider);
        repository.Entries.AddRange([kept, missing]);
        var handler = new DataTransferLeaveImportHandler(repository, TestTime.TimeProvider);

        var result = await handler.ImportAsync(
            new LeaveImportPayloadDto(
                [new LeaveEntryImportItemDto(kept.Id, kept.LeaveDate, kept.EntryType, null, kept.Notes, kept.LeaveType, null, null, false)]),
            new DataTransferImportOptions(DataTransferImportMode.Replace, AllowDeletes: true));

        Assert.False(kept.IsDeleted);
        Assert.True(missing.IsDeleted);
        Assert.True(repository.SaveChangesCalled);
        Assert.Contains(result.Warnings, x => x.Code == "WARN_REPLACE_SOFT_DELETED_MISSING");
    }

    private sealed class WorkLocationRepository : IWorkLocationRepository
    {
        public List<WorkLocationEntry> Entries { get; } = [];
        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<WorkLocationEntry>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkLocationEntry>>(Entries.Where(x => !x.IsDeleted).ToList());

        public Task<IReadOnlyList<WorkLocationEntry>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkLocationEntry>>(Entries);

        public Task<WorkLocationEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entries.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

        public Task<WorkLocationEntry?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entries.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<WorkLocationEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkLocationEntry>>(Entries.Where(x => !x.IsDeleted).ToList());

        public Task<bool> ExistsForDateAsync(DateTime workDate, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entries.Any(x => !x.IsDeleted && x.WorkDate.Date == workDate.Date && x.Id != excludingId));

        public Task AddAsync(WorkLocationEntry entity, CancellationToken cancellationToken = default)
        {
            Entries.Add(entity);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class LeaveRepository : ILeaveRepository
    {
        public List<LeaveEntry> Entries { get; } = [];
        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<LeaveEntry>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LeaveEntry>>(Entries.Where(x => !x.IsDeleted).ToList());

        public Task<IReadOnlyList<LeaveEntry>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LeaveEntry>>(Entries);

        public Task<LeaveEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entries.FirstOrDefault(x => x.Id == id && !x.IsDeleted));

        public Task<LeaveEntry?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entries.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<LeaveEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LeaveEntry>>(Entries.Where(x => !x.IsDeleted).ToList());

        public Task<bool> ExistsForDateAsync(DateTime leaveDate, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Entries.Any(x => !x.IsDeleted && x.LeaveDate.Date == leaveDate.Date && x.Id != excludingId));

        public Task AddAsync(LeaveEntry entity, CancellationToken cancellationToken = default)
        {
            Entries.Add(entity);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}