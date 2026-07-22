namespace TaxExpenseTracker.Api.Models;

public class CreateExpenseDto
{
    public string Item { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Bank { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid SourceId { get; set; }
    public List<Guid> TagIds { get; set; } = [];
}