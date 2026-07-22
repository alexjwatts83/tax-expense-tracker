namespace TaxExpenseTracker.Api.Models;

public class TaxExpense
{
    public Guid Id { get; set; }
    public string Item { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Bank { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid SourceId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Tracker? Source { get; set; }
    public ICollection<TaxExpenseTag> TaxExpenseTags { get; set; } = new List<TaxExpenseTag>();
}