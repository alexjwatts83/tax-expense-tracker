using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.Leave;

public sealed class LeaveService : ILeaveService
{
    private readonly ILeaveRepository _leaveRepository;

    public LeaveService(ILeaveRepository leaveRepository)
    {
        _leaveRepository = leaveRepository;
    }

    public async Task<IReadOnlyList<LeaveReadDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _leaveRepository.GetAllAsync(cancellationToken);

        return entries
            .OrderByDescending(x => x.LeaveDate)
            .Select(ToReadDto)
            .ToList();
    }

    public async Task<IReadOnlyList<LeaveReadDto>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var entries = await _leaveRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);

        return entries
            .OrderByDescending(x => x.LeaveDate)
            .Select(ToReadDto)
            .ToList();
    }

    public async Task<LeaveReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _leaveRepository.GetByIdAsync(id, cancellationToken);

        return entry is null ? null : ToReadDto(entry);
    }

    public async Task<LeaveReadDto> CreateAsync(CreateLeaveCommand command, CancellationToken cancellationToken = default)
    {
        var entry = LeaveEntry.Create(command.LeaveDate, command.EntryType, command.SpecificHours, command.Notes);

        await _leaveRepository.AddAsync(entry, cancellationToken);
        await _leaveRepository.SaveChangesAsync(cancellationToken);

        return ToReadDto(entry);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateLeaveCommand command, CancellationToken cancellationToken = default)
    {
        var entry = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        entry.Update(command.LeaveDate, command.EntryType, command.SpecificHours, command.Notes);
        await _leaveRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _leaveRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        entry.SoftDelete();
        await _leaveRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _leaveRepository.GetByIdIncludingDeletedAsync(id, cancellationToken);
        if (entry is null || !entry.IsDeleted)
        {
            return false;
        }

        entry.Restore();
        await _leaveRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<DayEntrySummaryDto> GetSummaryAsync(SummaryView view, DateTime date, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = SummaryPeriod.GetBounds(date, view);

        var entries = await _leaveRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);

        return new DayEntrySummaryDto(
            fromDate,
            toDate,
            entries.Sum(x => x.HoursWorked),
            entries.Select(x => x.LeaveDate.Date).Distinct().Count(),
            entries.Count);
    }

    private static LeaveReadDto ToReadDto(LeaveEntry entry)
    {
        return new LeaveReadDto(
            entry.Id,
            entry.LeaveDate,
            entry.EntryType,
            entry.HoursWorked,
            entry.Notes,
            entry.CreatedAt,
            entry.UpdatedAt);
    }
}