namespace TaxExpenseTracker.Api.Models;

public class CreateTrackerDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}