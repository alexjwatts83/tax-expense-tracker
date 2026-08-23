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
        var hoursWorked = DayEntryHours.Resolve(entryType, specificHours);

        return new LeaveEntry
        {
            Id = Guid.NewGuid(),
            LeaveDate = ValidateDate(leaveDate),
            LeaveType = ValidateLeaveType(leaveType),
            EntryType = entryType,
            HoursWorked = hoursWorked,
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
        var hoursWorked = DayEntryHours.Resolve(entryType, specificHours);
        LeaveDate = ValidateDate(leaveDate);
        LeaveType = ValidateLeaveType(leaveType);
        EntryType = entryType;
        HoursWorked = hoursWorked;
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