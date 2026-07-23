namespace TaxExpenseTracker.Domain.Entities;

public class TaxExpenseTag : Entity
{
    public Guid TaxExpenseId { get; set; }
    public Guid TagId { get; set; }

    public TaxExpense? TaxExpense { get; set; }
    public Tag? Tag { get; set; }

    public static TaxExpenseTag Create(Guid taxExpenseId, Guid tagId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(taxExpenseId, Guid.Empty, nameof(taxExpenseId));
        ArgumentOutOfRangeException.ThrowIfEqual(tagId, Guid.Empty, nameof(tagId));

        return new TaxExpenseTag
        {
            Id = Guid.NewGuid(),
            TaxExpenseId = taxExpenseId,
            TagId = tagId,
        };
    }
}