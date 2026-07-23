namespace TaxExpenseTracker.Domain.Entities;

public class PublicHoliday : Entity
{
    public DateTime HolidayDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Source { get; set; }
    public bool IsImported { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static PublicHoliday Create(
        DateTime holidayDate,
        string name,
        string? source,
        bool isImported,
        DateTime? utcNow = null)
    {
        return new PublicHoliday
        {
            Id = Guid.NewGuid(),
            HolidayDate = ValidateDate(holidayDate),
            Name = NormalizeRequired(name, nameof(Name)),
            Source = NormalizeOptional(source),
            IsImported = isImported,
            CreatedAt = utcNow ?? DateTime.UtcNow,
        };
    }

    public void Rename(string name)
    {
        Name = NormalizeRequired(name, nameof(Name));
    }

    private static DateTime ValidateDate(DateTime date)
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