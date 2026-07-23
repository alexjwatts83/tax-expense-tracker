namespace TaxExpenseTracker.Domain.Entities;

public abstract class SoftDeletableEntity : Entity, ISoftDeletableEntity
{
    public bool IsDeleted { get; set; }

    public virtual void SoftDelete(DateTime? utcNow = null)
    {
        IsDeleted = true;
    }

    public virtual void Restore(DateTime? utcNow = null)
    {
        IsDeleted = false;
    }
}