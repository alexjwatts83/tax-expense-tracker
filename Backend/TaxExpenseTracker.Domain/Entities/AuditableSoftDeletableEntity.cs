namespace TaxExpenseTracker.Domain.Entities;

public abstract class AuditableSoftDeletableEntity : SoftDeletableEntity
{
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public override void SoftDelete(DateTime? utcNow = null)
    {
        base.SoftDelete(utcNow);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
    }

    public override void Restore(DateTime? utcNow = null)
    {
        base.Restore(utcNow);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
    }
}