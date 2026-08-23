namespace TaxExpenseTracker.Domain.Entities;

public class Bank : AuditableSoftDeletableEntity
{
    public string Name { get; private set; } = string.Empty;

    public ICollection<TaxExpense> Expenses { get; private set; } = new List<TaxExpense>();

    public static Bank Create(string name, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return new Bank
        {
            Id = Guid.NewGuid(),
            Name = NormalizeRequired(name, nameof(Name)),
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false,
        };
    }

    public static Bank Import(Guid id, string name, TimeProvider timeProvider, DateTime? createdAt = null, DateTime? updatedAt = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty, nameof(id));
        var bank = Create(name, timeProvider);
        bank.Id = id;
        bank.RestoreAuditTimestamps(createdAt, updatedAt);
        return bank;
    }

    public void Rename(string name, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        Name = NormalizeRequired(name, nameof(Name));
        UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
    }

    public void ApplyImport(string name, TimeProvider timeProvider, DateTime? createdAt, DateTime? updatedAt)
    {
        Rename(name, timeProvider);
        RestoreAuditTimestamps(createdAt, updatedAt);
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, fieldName);
        return value.Trim();
    }
}