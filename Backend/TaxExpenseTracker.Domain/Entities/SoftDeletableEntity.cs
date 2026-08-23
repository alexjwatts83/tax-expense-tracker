namespace TaxExpenseTracker.Domain.Entities;

public abstract class SoftDeletableEntity : Entity, ISoftDeletableEntity
{
    public bool IsDeleted { get; protected set; }

    public virtual void SoftDelete(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        IsDeleted = true;
    }

    public virtual void Restore(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        IsDeleted = false;
    }
}