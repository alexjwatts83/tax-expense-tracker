using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.PublicHolidays;

namespace TaxExpenseTracker.Application.WorkFromHome;

public sealed class WorkFromHomeService : IWorkFromHomeService
{
    private const string StatusCreated = "Created";
    private const string StatusSkippedDuplicate = "SkippedDuplicate";
    private const string StatusFailedValidation = "FailedValidation";
    private const string StatusFailedConflict = "FailedConflict";

    private readonly IWorkFromHomeRepository _workFromHomeRepository;
    private readonly IPublicHolidayRepository _publicHolidayRepository;
    private readonly TimeProvider _timeProvider;

    public WorkFromHomeService(
        IWorkFromHomeRepository workFromHomeRepository,
        IPublicHolidayRepository publicHolidayRepository,
        TimeProvider timeProvider)
    {
        _workFromHomeRepository = workFromHomeRepository;
        _publicHolidayRepository = publicHolidayRepository;
        _timeProvider = timeProvider;
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
        var existsForDate = await _workFromHomeRepository.ExistsForDateAsync(command.WorkDate, cancellationToken: cancellationToken);
        if (existsForDate)
            ThrowHelper.InvalidOperation("A work-from-home entry already exists for this date.");

        var entry = WorkFromHomeEntry.Create(command.WorkDate, command.EntryType, command.SpecificHours, command.Notes, _timeProvider);

        await _workFromHomeRepository.AddAsync(entry, cancellationToken);
        await _workFromHomeRepository.SaveChangesAsync(cancellationToken);

        return ToReadDto(entry);
    }

    public async Task<BatchCreateWorkFromHomeResult> BatchCreateAsync(
        IReadOnlyList<CreateWorkFromHomeCommand> commands,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commands);

        if (commands.Count == 0)
        {
            return new BatchCreateWorkFromHomeResult(0, 0, 0, 0, []);
        }

        var minDate = commands.Min(x => x.WorkDate).Date;
        var maxDate = commands.Max(x => x.WorkDate).Date;
        var holidays = await _publicHolidayRepository.GetByDateRangeAsync(minDate, maxDate, cancellationToken);
        var holidayDates = holidays
            .Select(x => x.HolidayDate.Date)
            .ToHashSet();

        var seenDates = new HashSet<DateTime>();
        var results = new List<BatchCreateWorkFromHomeItemResult>(commands.Count);
        var createdCount = 0;
        var skippedCount = 0;

        foreach (var command in commands)
        {
            var workDate = command.WorkDate.Date;

            if (holidayDates.Contains(workDate))
            {
                results.Add(new BatchCreateWorkFromHomeItemResult(
                    workDate,
                    command.EntryType,
                    command.SpecificHours,
                    command.Notes,
                    StatusFailedConflict,
                    "Cannot create work-from-home on a public holiday.",
                    null));
                continue;
            }

            if (!seenDates.Add(workDate))
            {
                skippedCount += 1;
                results.Add(new BatchCreateWorkFromHomeItemResult(
                    workDate,
                    command.EntryType,
                    command.SpecificHours,
                    command.Notes,
                    StatusSkippedDuplicate,
                    "This batch already includes an entry for the same date.",
                    null));
                continue;
            }

            var existsForDate = await _workFromHomeRepository.ExistsForDateAsync(workDate, cancellationToken: cancellationToken);
            if (existsForDate)
            {
                skippedCount += 1;
                results.Add(new BatchCreateWorkFromHomeItemResult(
                    workDate,
                    command.EntryType,
                    command.SpecificHours,
                    command.Notes,
                    StatusSkippedDuplicate,
                    "A work-from-home entry already exists for this date.",
                    null));
                continue;
            }

            try
            {
                var entry = WorkFromHomeEntry.Create(workDate, command.EntryType, command.SpecificHours, command.Notes, _timeProvider);
                await _workFromHomeRepository.AddAsync(entry, cancellationToken);
                await _workFromHomeRepository.SaveChangesAsync(cancellationToken);

                createdCount += 1;
                results.Add(new BatchCreateWorkFromHomeItemResult(
                    workDate,
                    command.EntryType,
                    command.SpecificHours,
                    command.Notes,
                    StatusCreated,
                    null,
                    ToReadDto(entry)));
            }
            catch (ArgumentException ex)
            {
                results.Add(new BatchCreateWorkFromHomeItemResult(
                    workDate,
                    command.EntryType,
                    command.SpecificHours,
                    command.Notes,
                    StatusFailedValidation,
                    ex.Message,
                    null));
            }
            catch (InvalidOperationException ex)
            {
                results.Add(new BatchCreateWorkFromHomeItemResult(
                    workDate,
                    command.EntryType,
                    command.SpecificHours,
                    command.Notes,
                    StatusFailedConflict,
                    ex.Message,
                    null));
            }
        }

        var failedCount = results.Count - createdCount - skippedCount;

        return new BatchCreateWorkFromHomeResult(
            commands.Count,
            createdCount,
            skippedCount,
            failedCount,
            results);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateWorkFromHomeCommand command, CancellationToken cancellationToken = default)
    {
        var entry = await _workFromHomeRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        var existsForDate = await _workFromHomeRepository.ExistsForDateAsync(command.WorkDate, id, cancellationToken);
        if (existsForDate)
            ThrowHelper.InvalidOperation("A work-from-home entry already exists for this date.");

        entry.Update(command.WorkDate, command.EntryType, command.SpecificHours, command.Notes, _timeProvider);
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

        entry.SoftDelete(_timeProvider);
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

        entry.Restore(_timeProvider);
        await _workFromHomeRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<DayEntrySummaryDto> GetSummaryAsync(SummaryView view, DateTime date, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = SummaryPeriod.GetBounds(date, view);

        var entries = await _workFromHomeRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);
        var holidays = await _publicHolidayRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);

        return new DayEntrySummaryDto(
            fromDate,
            toDate,
            entries.Sum(x => x.HoursWorked),
            entries.Select(x => x.WorkDate.Date).Distinct().Count(),
            entries.Count,
            holidays
                .OrderBy(x => x.HolidayDate)
                .ThenBy(x => x.Name)
                .Select(x => new HolidayMarkerDto(x.HolidayDate.Date, x.Name))
                .ToList());
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