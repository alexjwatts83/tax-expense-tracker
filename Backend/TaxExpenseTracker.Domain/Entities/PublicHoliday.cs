namespace TaxExpenseTracker.Domain.Entities;

public class PublicHoliday : AuditableEntity
{
    public DateOnly HolidayDate { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Source { get; private set; }
    public bool IsImported { get; private set; }
    public bool CanBeWorkedOn { get; private set; }

    public static PublicHoliday Create(
        DateOnly holidayDate,
        string name,
        string? source,
        bool isImported,
        TimeProvider timeProvider,
        bool canBeWorkedOn = false)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return new PublicHoliday
        {
            Id = Guid.NewGuid(),
            HolidayDate = ValidateDate(holidayDate),
            Name = NormalizeRequired(name, nameof(Name)),
            Source = NormalizeOptional(source),
            IsImported = isImported,
            CanBeWorkedOn = canBeWorkedOn,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public static PublicHoliday Import(
        Guid id,
        DateOnly holidayDate,
        string name,
        string? source,
        bool isImported,
        TimeProvider timeProvider,
        bool canBeWorkedOn = false,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty, nameof(id));
        var holiday = Create(holidayDate, name, source, isImported, timeProvider, canBeWorkedOn);
        holiday.Id = id;
        holiday.RestoreAuditTimestamps(createdAt, updatedAt);
        return holiday;
    }

    public void Update(
        DateOnly holidayDate,
        string name,
        string? source,
        bool isImported,
        bool canBeWorkedOn,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var validatedDate = ValidateDate(holidayDate);
        var normalizedName = NormalizeRequired(name, nameof(Name));
        HolidayDate = validatedDate;
        Name = normalizedName;
        Source = NormalizeOptional(source);
        IsImported = isImported;
        CanBeWorkedOn = canBeWorkedOn;
        UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
    }

    public void SetWorkable(bool canBeWorkedOn, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        CanBeWorkedOn = canBeWorkedOn;
        UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
    }

    public void ApplyImport(
        DateOnly holidayDate,
        string name,
        string? source,
        bool canBeWorkedOn,
        TimeProvider timeProvider,
        DateTime? createdAt,
        DateTime? updatedAt)
    {
        Update(holidayDate, name, source, true, canBeWorkedOn, timeProvider);
        RestoreAuditTimestamps(createdAt, updatedAt);
    }

    private static DateOnly ValidateDate(DateOnly date)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(date, default, nameof(date));
        return date;
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, fieldName);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}