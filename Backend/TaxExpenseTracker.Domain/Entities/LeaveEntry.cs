namespace TaxExpenseTracker.Domain.Entities;

public class LeaveEntry : AuditableSoftDeletableEntity
{
    public DateOnly LeaveDate { get; private set; }
    public LeaveType LeaveType { get; private set; } = LeaveType.Annual;
    public DayEntryType EntryType { get; private set; }
    public decimal HoursWorked { get; private set; }
    public string? Notes { get; private set; }

    public static LeaveEntry Create(
        DateOnly leaveDate,
        DayEntryType entryType,
        decimal? specificHours,
        string? notes,
        TimeProvider timeProvider,
        LeaveType leaveType = LeaveType.Annual)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new LeaveEntry
        {
            Id = Guid.NewGuid(),
            LeaveDate = ValidateDate(leaveDate),
            LeaveType = ValidateLeaveType(leaveType),
            EntryType = ValidateEntryType(entryType),
            HoursWorked = ResolveHours(entryType, specificHours),
            Notes = NormalizeOptional(notes),
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public static LeaveEntry Import(
        Guid id,
        DateOnly leaveDate,
        DayEntryType entryType,
        decimal? specificHours,
        string? notes,
        TimeProvider timeProvider,
        LeaveType leaveType = LeaveType.Annual)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty, nameof(id));
        var entry = Create(leaveDate, entryType, specificHours, notes, timeProvider, leaveType);
        entry.Id = id;
        return entry;
    }

    public void Update(
        DateOnly leaveDate,
        DayEntryType entryType,
        decimal? specificHours,
        string? notes,
        TimeProvider timeProvider,
        LeaveType leaveType = LeaveType.Annual)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        LeaveDate = ValidateDate(leaveDate);
        LeaveType = ValidateLeaveType(leaveType);
        EntryType = ValidateEntryType(entryType);
        HoursWorked = ResolveHours(entryType, specificHours);
        Notes = NormalizeOptional(notes);
        UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
    }

    private static LeaveType ValidateLeaveType(LeaveType leaveType)
    {
        if (!Enum.IsDefined(leaveType))
        {
            throw new ArgumentOutOfRangeException(nameof(leaveType), "Leave type is invalid.");
        }

        return leaveType;
    }

    private static DayEntryType ValidateEntryType(DayEntryType entryType)
    {
        if (!Enum.IsDefined(entryType))
        {
            throw new ArgumentOutOfRangeException(nameof(entryType), "Entry type is invalid.");
        }

        return entryType;
    }

    private static DateOnly ValidateDate(DateOnly date)
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