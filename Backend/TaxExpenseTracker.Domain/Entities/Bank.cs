namespace TaxExpenseTracker.Domain.Entities;

public class Bank
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TaxExpense> Expenses { get; set; } = new List<TaxExpense>();

    public static Bank Create(string name, DateTime? utcNow = null)
    {
        return new Bank
        {
            Id = Guid.NewGuid(),
            Name = NormalizeRequired(name, nameof(Name)),
            CreatedAt = utcNow ?? DateTime.UtcNow,
            IsDeleted = false,
        };
    }

    public void Rename(string name)
    {
        Name = NormalizeRequired(name, nameof(Name));
    }

    public void SoftDelete()
    {
        IsDeleted = true;
    }

    public void Restore()
    {
        IsDeleted = false;
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
}