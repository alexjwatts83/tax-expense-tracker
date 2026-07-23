namespace TaxExpenseTracker.Domain.Entities;

public abstract class AuditableSoftDeletableEntity : AuditableEntity, ISoftDeletableEntity
{
    public bool IsDeleted { get; set; }

    public virtual void SoftDelete(DateTime? utcNow = null)
    {
        IsDeleted = true;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
    }

    public virtual void Restore(DateTime? utcNow = null)
    {
        IsDeleted = false;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
    }
}