using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Api.Models;

public sealed class WorkLocationBatchResultDto
{
    public int TotalRequested { get; set; }
    public int CreatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public IReadOnlyList<WorkLocationBatchItemResultDto> Results { get; set; } = [];
}

public sealed class WorkLocationBatchItemResultDto
{
    public DateTime WorkDate { get; set; }
    public WorkLocationType WorkLocation { get; set; }
    public DayEntryType EntryType { get; set; }
    public decimal? SpecificHours { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public WorkLocationDto? Entry { get; set; }
}
