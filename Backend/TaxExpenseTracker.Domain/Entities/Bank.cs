namespace TaxExpenseTracker.Domain.Entities;

public class Bank : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
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

    private static string NormalizeRequired(string value, string fieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, fieldName);
        return value.Trim();
    }
}