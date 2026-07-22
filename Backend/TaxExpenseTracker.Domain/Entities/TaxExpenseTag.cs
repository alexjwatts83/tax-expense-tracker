namespace TaxExpenseTracker.Domain.Entities;

public class TaxExpenseTag
{
    public Guid Id { get; set; }
    public Guid TaxExpenseId { get; set; }
    public Guid TagId { get; set; }

    public TaxExpense? TaxExpense { get; set; }
    public Tag? Tag { get; set; }

    public static TaxExpenseTag Create(Guid taxExpenseId, Guid tagId)
    {
        if (taxExpenseId == Guid.Empty)
        {
            throw new ArgumentException("TaxExpenseId is required.", nameof(taxExpenseId));
        }

        if (tagId == Guid.Empty)
        {
            throw new ArgumentException("TagId is required.", nameof(tagId));
        }

        return new TaxExpenseTag
        {
            Id = Guid.NewGuid(),
            TaxExpenseId = taxExpenseId,
            TagId = tagId,
        };
    }
}