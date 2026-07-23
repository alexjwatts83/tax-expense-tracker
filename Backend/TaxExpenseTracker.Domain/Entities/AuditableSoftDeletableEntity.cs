namespace TaxExpenseTracker.Domain.Entities;

public abstract class AuditableSoftDeletableEntity : AuditableEntity, ISoftDeletableEntity
{
    public bool IsDeleted { get; set; }

    public virtual void SoftDelete(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        IsDeleted = true;
        UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
    }

    public virtual void Restore(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        IsDeleted = false;
        UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
    }
}