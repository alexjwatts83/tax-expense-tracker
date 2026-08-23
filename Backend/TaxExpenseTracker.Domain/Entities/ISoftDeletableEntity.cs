namespace TaxExpenseTracker.Domain.Entities;

public interface ISoftDeletableEntity : IEntity
{
    bool IsDeleted { get; }
    void SoftDelete(TimeProvider timeProvider);
    void Restore(TimeProvider timeProvider);
}