namespace TaxExpenseTracker.Domain.Entities;

public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }

    protected void RestoreAuditTimestamps(DateTime? createdAt, DateTime? updatedAt)
    {
        CreatedAt = createdAt ?? CreatedAt;
        UpdatedAt = updatedAt ?? createdAt ?? UpdatedAt;
    }
}