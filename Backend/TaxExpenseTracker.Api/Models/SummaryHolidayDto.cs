namespace TaxExpenseTracker.Api.Models;

public sealed class SummaryHolidayDto
{
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
}