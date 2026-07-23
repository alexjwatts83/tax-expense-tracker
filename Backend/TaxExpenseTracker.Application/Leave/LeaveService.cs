using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.PublicHolidays;

namespace TaxExpenseTracker.Application.Leave;

public sealed class LeaveService : ILeaveService
{
    private readonly ILeaveRepository _leaveRepository;
    private readonly IPublicHolidayRepository _publicHolidayRepository;
    private readonly TimeProvider _timeProvider;

    public LeaveService(
        ILeaveRepository leaveRepository,
        IPublicHolidayRepository publicHolidayRepository,
        TimeProvider timeProvider)
    {
        _leaveRepository = leaveRepository;
        _publicHolidayRepository = publicHolidayRepository;
        _timeProvider = timeProvider;
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
        var existsForDate = await _leaveRepository.ExistsForDateAsync(command.LeaveDate, cancellationToken: cancellationToken);
        if (existsForDate)
            ThrowHelper.InvalidOperation("A leave entry already exists for this date.");

        var entry = LeaveEntry.Create(command.LeaveDate, command.EntryType, command.SpecificHours, command.Notes, _timeProvider);

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

        var existsForDate = await _leaveRepository.ExistsForDateAsync(command.LeaveDate, id, cancellationToken);
        if (existsForDate)
            ThrowHelper.InvalidOperation("A leave entry already exists for this date.");

        entry.Update(command.LeaveDate, command.EntryType, command.SpecificHours, command.Notes, _timeProvider);
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

        entry.SoftDelete(_timeProvider);
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

        entry.Restore(_timeProvider);
        await _leaveRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<DayEntrySummaryDto> GetSummaryAsync(SummaryView view, DateTime date, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = SummaryPeriod.GetBounds(date, view);

        var entries = await _leaveRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);
        var holidays = await _publicHolidayRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);

        return new DayEntrySummaryDto(
            fromDate,
            toDate,
            entries.Sum(x => x.HoursWorked),
            entries.Select(x => x.LeaveDate.Date).Distinct().Count(),
            entries.Count,
            holidays
                .OrderBy(x => x.HolidayDate)
                .ThenBy(x => x.Name)
                .Select(x => new HolidayMarkerDto(x.HolidayDate.Date, x.Name))
                .ToList());
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