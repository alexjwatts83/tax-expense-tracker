namespace TaxExpenseTracker.Api.Models;

public sealed class LeaveSummaryDto
{
    public string View { get; set; } = string.Empty;
    public DateTime AnchorDate { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalHours { get; set; }
    public int TotalDays { get; set; }
    public int EntryCount { get; set; }
}