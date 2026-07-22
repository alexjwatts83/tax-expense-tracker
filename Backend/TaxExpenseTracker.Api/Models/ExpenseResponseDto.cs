namespace TaxExpenseTracker.Api.Models;

public class ExpenseResponseDto
{
    public Guid Id { get; set; }
    public string Item { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public Guid BankId { get; set; }
    public BankDto? Bank { get; set; }
    public decimal Price { get; set; }
    public Guid SourceId { get; set; }
    public TrackerDto? Source { get; set; }
    public List<TagDto> Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}