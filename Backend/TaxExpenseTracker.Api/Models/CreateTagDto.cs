namespace TaxExpenseTracker.Api.Models;

public class CreateTagDto
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}