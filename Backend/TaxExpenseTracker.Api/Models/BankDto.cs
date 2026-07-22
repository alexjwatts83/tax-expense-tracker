namespace TaxExpenseTracker.Api.Models;

public class BankDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}