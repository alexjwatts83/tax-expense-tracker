namespace TaxExpenseTracker.Domain.Entities;

public class Tracker : AuditableSoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<TaxExpense> Expenses { get; set; } = new List<TaxExpense>();

    public static Tracker Create(string name, string? description, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new Tracker
        {
            Id = Guid.NewGuid(),
            Name = NormalizeRequired(name, nameof(Name)),
            Description = NormalizeOptional(description),
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        };
    }

    public void Rename(string name, string? description, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        Name = NormalizeRequired(name, nameof(Name));
        Description = NormalizeOptional(description);
        UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
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