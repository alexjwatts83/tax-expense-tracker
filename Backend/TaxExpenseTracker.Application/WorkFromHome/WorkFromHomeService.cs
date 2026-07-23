using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.WorkFromHome;

public sealed class WorkFromHomeService : IWorkFromHomeService
{
    private readonly IWorkFromHomeRepository _workFromHomeRepository;

    public WorkFromHomeService(IWorkFromHomeRepository workFromHomeRepository)
    {
        _workFromHomeRepository = workFromHomeRepository;
    }

    public async Task<IReadOnlyList<WorkFromHomeReadDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _workFromHomeRepository.GetAllAsync(cancellationToken);

        return entries
            .OrderByDescending(x => x.WorkDate)
            .Select(ToReadDto)
            .ToList();
    }

    public async Task<IReadOnlyList<WorkFromHomeReadDto>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var entries = await _workFromHomeRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);

        return entries
            .OrderByDescending(x => x.WorkDate)
            .Select(ToReadDto)
            .ToList();
    }

    public async Task<WorkFromHomeReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _workFromHomeRepository.GetByIdAsync(id, cancellationToken);

        return entry is null ? null : ToReadDto(entry);
    }

    public async Task<WorkFromHomeReadDto> CreateAsync(CreateWorkFromHomeCommand command, CancellationToken cancellationToken = default)
    {
        var entry = WorkFromHomeEntry.Create(command.WorkDate, command.EntryType, command.SpecificHours, command.Notes);

        await _workFromHomeRepository.AddAsync(entry, cancellationToken);
        await _workFromHomeRepository.SaveChangesAsync(cancellationToken);

        return ToReadDto(entry);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateWorkFromHomeCommand command, CancellationToken cancellationToken = default)
    {
        var entry = await _workFromHomeRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        entry.Update(command.WorkDate, command.EntryType, command.SpecificHours, command.Notes);
        await _workFromHomeRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _workFromHomeRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        entry.SoftDelete();
        await _workFromHomeRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _workFromHomeRepository.GetByIdIncludingDeletedAsync(id, cancellationToken);
        if (entry is null || !entry.IsDeleted)
        {
            return false;
        }

        entry.Restore();
        await _workFromHomeRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<DayEntrySummaryDto> GetSummaryAsync(SummaryView view, DateTime date, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = SummaryPeriod.GetBounds(date, view);

        var entries = await _workFromHomeRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);

        return new DayEntrySummaryDto(
            fromDate,
            toDate,
            entries.Sum(x => x.HoursWorked),
            entries.Select(x => x.WorkDate.Date).Distinct().Count(),
            entries.Count);
    }

    private static WorkFromHomeReadDto ToReadDto(WorkFromHomeEntry entry)
    {
        return new WorkFromHomeReadDto(
            entry.Id,
            entry.WorkDate,
            entry.EntryType,
            entry.HoursWorked,
            entry.Notes,
            entry.CreatedAt,
            entry.UpdatedAt);
    }
}