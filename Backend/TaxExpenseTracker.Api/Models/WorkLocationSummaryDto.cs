namespace TaxExpenseTracker.Api.Models;

public sealed class WorkLocationSummaryDto
{
    public string View { get; set; } = string.Empty;
    public DateOnly AnchorDate { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal TotalHours { get; set; }
    public int TotalDays { get; set; }
    public int EntryCount { get; set; }
    public List<SummaryHolidayDto> Holidays { get; set; } = [];
}