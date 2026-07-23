namespace TaxExpenseTracker.Domain.Entities;

public interface ISoftDeletableEntity : IEntity
{
    bool IsDeleted { get; set; }
    void SoftDelete(DateTime? utcNow = null);
    void Restore(DateTime? utcNow = null);
}