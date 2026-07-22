namespace TaxExpenseTracker.Domain.Entities;

public class TaxExpenseTag
{
    public Guid Id { get; set; }
    public Guid TaxExpenseId { get; set; }
    public Guid TagId { get; set; }

    public TaxExpense? TaxExpense { get; set; }
    public Tag? Tag { get; set; }
}