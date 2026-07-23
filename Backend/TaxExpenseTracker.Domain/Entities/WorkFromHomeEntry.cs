namespace TaxExpenseTracker.Domain.Entities;

public class WorkFromHomeEntry : AuditableSoftDeletableEntity
{
    public DateTime WorkDate { get; set; }
    public WorkLocationType WorkLocation { get; set; }
    public DayEntryType EntryType { get; set; }
    public decimal HoursWorked { get; set; }
    public string? Notes { get; set; }

    public static WorkFromHomeEntry Create(
        DateTime workDate,
        DayEntryType entryType,
        decimal? specificHours,
        string? notes,
        TimeProvider timeProvider,
        WorkLocationType workLocation = WorkLocationType.Wfh)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new WorkFromHomeEntry
        {
            Id = Guid.NewGuid(),
            WorkDate = ValidateDate(workDate),
            WorkLocation = ValidateWorkLocation(workLocation),
            EntryType = ValidateEntryType(entryType),
            HoursWorked = ResolveHours(entryType, specificHours),
            Notes = NormalizeOptional(notes),
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(
        DateTime workDate,
        DayEntryType entryType,
        decimal? specificHours,
        string? notes,
        TimeProvider timeProvider,
        WorkLocationType workLocation = WorkLocationType.Wfh)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        WorkDate = ValidateDate(workDate);
        WorkLocation = ValidateWorkLocation(workLocation);
        EntryType = ValidateEntryType(entryType);
        HoursWorked = ResolveHours(entryType, specificHours);
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

    private static DayEntryType ValidateEntryType(DayEntryType entryType)
    {
        if (!Enum.IsDefined(entryType))
        {
            throw new ArgumentOutOfRangeException(nameof(entryType), "Entry type is invalid.");
        }

        return entryType;
    }

    private static DateTime ValidateDate(DateTime date)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(date, default, nameof(date));
        return date;
    }

    private static decimal ResolveHours(DayEntryType entryType, decimal? specificHours)
    {
        return entryType switch
        {
            DayEntryType.FullDay => 7.6m,
            DayEntryType.HalfDay => 3.8m,
            DayEntryType.SpecificHours => ValidateSpecificHours(specificHours),
            _ => throw new ArgumentOutOfRangeException(nameof(entryType), "Entry type is invalid."),
        };
    }

    private static decimal ValidateSpecificHours(decimal? specificHours)
    {
        if (!specificHours.HasValue || specificHours.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(specificHours), "Specific hours must be greater than 0.");
        }

        if (specificHours.Value > 24)
        {
            throw new ArgumentOutOfRangeException(nameof(specificHours), "Specific hours must be 24 or less.");
        }

        return specificHours.Value;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}