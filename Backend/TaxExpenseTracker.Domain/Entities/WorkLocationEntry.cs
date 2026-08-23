namespace TaxExpenseTracker.Domain.Entities;

public class WorkLocationEntry : AuditableSoftDeletableEntity
{
    public DateOnly WorkDate { get; private set; }
    public WorkLocationType WorkLocation { get; private set; }
    public DayEntryType EntryType { get; private set; }
    public decimal HoursWorked { get; private set; }
    public string? Notes { get; private set; }

    public static WorkLocationEntry Create(
        DateOnly workDate,
        DayEntryType entryType,
        decimal? specificHours,
        string? notes,
        TimeProvider timeProvider,
        WorkLocationType workLocation = WorkLocationType.Wfh)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hoursWorked = DayEntryHours.Resolve(entryType, specificHours);

        return new WorkLocationEntry
        {
            Id = Guid.NewGuid(),
            WorkDate = ValidateDate(workDate),
            WorkLocation = ValidateWorkLocation(workLocation),
            EntryType = entryType,
            HoursWorked = hoursWorked,
            Notes = NormalizeOptional(notes),
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public static WorkLocationEntry Import(
        Guid id,
        DateOnly workDate,
        DayEntryType entryType,
        decimal? specificHours,
        string? notes,
        TimeProvider timeProvider,
        WorkLocationType workLocation = WorkLocationType.Wfh)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty, nameof(id));
        var entry = Create(workDate, entryType, specificHours, notes, timeProvider, workLocation);
        entry.Id = id;
        return entry;
    }

    public void Update(
        DateOnly workDate,
        DayEntryType entryType,
        decimal? specificHours,
        string? notes,
        TimeProvider timeProvider,
        WorkLocationType workLocation = WorkLocationType.Wfh)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var hoursWorked = DayEntryHours.Resolve(entryType, specificHours);
        WorkDate = ValidateDate(workDate);
        WorkLocation = ValidateWorkLocation(workLocation);
        EntryType = entryType;
        HoursWorked = hoursWorked;
        Notes = NormalizeOptional(notes);
        UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
    }

    private static WorkLocationType ValidateWorkLocation(WorkLocationType workLocation)
    {
        if (!Enum.IsDefined(workLocation))
        {
            throw new ArgumentOutOfRangeException(nameof(workLocation), "Work location is invalid.");
        }

        return workLocation;
    }

    private static DateOnly ValidateDate(DateOnly date)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(date, default, nameof(date));
        return date;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}