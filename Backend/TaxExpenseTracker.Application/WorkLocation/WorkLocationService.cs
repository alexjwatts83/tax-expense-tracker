using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.PublicHolidays;

namespace TaxExpenseTracker.Application.WorkLocation;

public sealed class WorkLocationService : IWorkLocationService
{
    private const string StatusCreated = "Created";
    private const string StatusSkippedDuplicate = "SkippedDuplicate";
    private const string StatusFailedValidation = "FailedValidation";
    private const string StatusFailedConflict = "FailedConflict";

    private readonly IWorkLocationRepository _workLocationRepository;
    private readonly IPublicHolidayRepository _publicHolidayRepository;
    private readonly TimeProvider _timeProvider;

    public WorkLocationService(
        IWorkLocationRepository workLocationRepository,
        IPublicHolidayRepository publicHolidayRepository,
        TimeProvider timeProvider)
    {
        _workLocationRepository = workLocationRepository;
        _publicHolidayRepository = publicHolidayRepository;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<WorkLocationReadDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _workLocationRepository.GetAllAsync(cancellationToken);

        return entries
            .OrderByDescending(x => x.WorkDate)
            .Select(ToReadDto)
            .ToList();
    }

    public async Task<IReadOnlyList<WorkLocationReadDto>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var entries = await _workLocationRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);

        return entries
            .OrderByDescending(x => x.WorkDate)
            .Select(ToReadDto)
            .ToList();
    }

    public async Task<WorkLocationReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _workLocationRepository.GetByIdAsync(id, cancellationToken);

        return entry is null ? null : ToReadDto(entry);
    }

    public async Task<WorkLocationReadDto> CreateAsync(CreateWorkLocationCommand command, CancellationToken cancellationToken = default)
    {
        var existsForDate = await _workLocationRepository.ExistsForDateAsync(command.WorkDate, cancellationToken: cancellationToken);
        if (existsForDate)
            ThrowHelper.InvalidOperation("A work-location entry already exists for this date.");

        var entry = WorkLocationEntry.Create(command.WorkDate, command.EntryType, command.SpecificHours, command.Notes, _timeProvider, command.WorkLocation);

        await _workLocationRepository.AddAsync(entry, cancellationToken);
        await _workLocationRepository.SaveChangesAsync(cancellationToken);

        return ToReadDto(entry);
    }

    public async Task<BatchCreateWorkLocationResult> BatchCreateAsync(
        IReadOnlyList<CreateWorkLocationCommand> commands,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commands);

        if (commands.Count == 0)
        {
            return new BatchCreateWorkLocationResult(0, 0, 0, 0, []);
        }

        var minDate = commands.Min(x => x.WorkDate).Date;
        var maxDate = commands.Max(x => x.WorkDate).Date;
        var holidays = await _publicHolidayRepository.GetByDateRangeAsync(minDate, maxDate, cancellationToken);
        var holidayDates = holidays
            .Where(x => !x.CanBeWorkedOn)
            .Select(x => x.HolidayDate.Date)
            .ToHashSet();

        var seenDates = new HashSet<DateTime>();
        var results = new List<BatchCreateWorkLocationItemResult>(commands.Count);
        var createdCount = 0;
        var skippedCount = 0;

        foreach (var command in commands)
        {
            var workDate = command.WorkDate.Date;

            if (holidayDates.Contains(workDate))
            {
                results.Add(new BatchCreateWorkLocationItemResult(
                    workDate,
                    command.WorkLocation,
                    command.EntryType,
                    command.SpecificHours,
                    command.Notes,
                    StatusFailedConflict,
                    "Cannot create work-location on a public holiday.",
                    null));
                continue;
            }

            if (!seenDates.Add(workDate))
            {
                skippedCount += 1;
                results.Add(new BatchCreateWorkLocationItemResult(
                    workDate,
                    command.WorkLocation,
                    command.EntryType,
                    command.SpecificHours,
                    command.Notes,
                    StatusSkippedDuplicate,
                    "This batch already includes an entry for the same date.",
                    null));
                continue;
            }

            var existsForDate = await _workLocationRepository.ExistsForDateAsync(workDate, cancellationToken: cancellationToken);
            if (existsForDate)
            {
                skippedCount += 1;
                results.Add(new BatchCreateWorkLocationItemResult(
                    workDate,
                    command.WorkLocation,
                    command.EntryType,
                    command.SpecificHours,
                    command.Notes,
                    StatusSkippedDuplicate,
                    "A work-location entry already exists for this date.",
                    null));
                continue;
            }

            try
            {
                var entry = WorkLocationEntry.Create(workDate, command.EntryType, command.SpecificHours, command.Notes, _timeProvider, command.WorkLocation);
                await _workLocationRepository.AddAsync(entry, cancellationToken);
                await _workLocationRepository.SaveChangesAsync(cancellationToken);

                createdCount += 1;
                results.Add(new BatchCreateWorkLocationItemResult(
                    workDate,
                    command.WorkLocation,
                    command.EntryType,
                    command.SpecificHours,
                    command.Notes,
                    StatusCreated,
                    null,
                    ToReadDto(entry)));
            }
            catch (ArgumentException ex)
            {
                results.Add(new BatchCreateWorkLocationItemResult(
                    workDate,
                    command.WorkLocation,
                    command.EntryType,
                    command.SpecificHours,
                    command.Notes,
                    StatusFailedValidation,
                    ex.Message,
                    null));
            }
            catch (InvalidOperationException ex)
            {
                results.Add(new BatchCreateWorkLocationItemResult(
                    workDate,
                    command.WorkLocation,
                    command.EntryType,
                    command.SpecificHours,
                    command.Notes,
                    StatusFailedConflict,
                    ex.Message,
                    null));
            }
        }

        var failedCount = results.Count - createdCount - skippedCount;

        return new BatchCreateWorkLocationResult(
            commands.Count,
            createdCount,
            skippedCount,
            failedCount,
            results);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateWorkLocationCommand command, CancellationToken cancellationToken = default)
    {
        var entry = await _workLocationRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        var existsForDate = await _workLocationRepository.ExistsForDateAsync(command.WorkDate, id, cancellationToken);
        if (existsForDate)
            ThrowHelper.InvalidOperation("A work-location entry already exists for this date.");

        entry.Update(command.WorkDate, command.EntryType, command.SpecificHours, command.Notes, _timeProvider, command.WorkLocation);
        await _workLocationRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _workLocationRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        entry.SoftDelete(_timeProvider);
        await _workLocationRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _workLocationRepository.GetByIdIncludingDeletedAsync(id, cancellationToken);
        if (entry is null || !entry.IsDeleted)
        {
            return false;
        }

        entry.Restore(_timeProvider);
        await _workLocationRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<DayEntrySummaryDto> GetSummaryAsync(SummaryView view, DateTime date, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = SummaryPeriod.GetBounds(date, view);

        var entries = await _workLocationRepository.GetByDateRangeAsync(fromDate, toDate, cancellationToken);
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
                .Select(x => new HolidayMarkerDto(x.HolidayDate.Date, x.Name, x.CanBeWorkedOn))
                .ToList());
    }

    private static WorkLocationReadDto ToReadDto(WorkLocationEntry entry)
    {
        return new WorkLocationReadDto(
            entry.Id,
            entry.WorkDate,
            entry.EntryType,
            entry.HoursWorked,
            entry.Notes,
            entry.CreatedAt,
            entry.UpdatedAt,
            entry.WorkLocation);
    }
}