namespace TaxExpenseTracker.Domain.Entities;

public class Tracker : AuditableSoftDeletableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public ICollection<TaxExpense> Expenses { get; private set; } = new List<TaxExpense>();

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

    public static Tracker Import(
        Guid id,
        string name,
        string? description,
        TimeProvider timeProvider,
        DateTime? createdAt = null,
        DateTime? updatedAt = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty, nameof(id));
        var tracker = Create(name, description, timeProvider);
        tracker.Id = id;
        tracker.RestoreAuditTimestamps(createdAt, updatedAt);
        return tracker;
    }

    public void ApplyImport(string name, string? description, TimeProvider timeProvider, DateTime? createdAt, DateTime? updatedAt)
    {
        Rename(name, description, timeProvider);
        RestoreAuditTimestamps(createdAt, updatedAt);
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