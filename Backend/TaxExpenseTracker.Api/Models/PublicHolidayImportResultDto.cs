namespace TaxExpenseTracker.Api.Models;

public sealed class PublicHolidayImportResultDto
{
    public int ImportedCount { get; set; }
    public int SkippedDuplicateCount { get; set; }
    public List<string> Warnings { get; set; } = [];
}
