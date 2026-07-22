namespace TaxExpenseTracker.Domain.Entities;

public class Tracker
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TaxExpense> Expenses { get; set; } = new List<TaxExpense>();

    public static Tracker Create(string name, string? description, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

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

    public void Rename(string name, string? description, DateTime? utcNow = null)
    {
        Name = NormalizeRequired(name, nameof(Name));
        Description = NormalizeOptional(description);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
    }

    public void SoftDelete(DateTime? utcNow = null)
    {
        IsDeleted = true;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}