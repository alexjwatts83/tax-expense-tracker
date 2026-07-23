using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Api.Models;

public sealed class WorkFromHomeBatchResultDto
{
    public int TotalRequested { get; set; }
    public int CreatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public IReadOnlyList<WorkFromHomeBatchItemResultDto> Results { get; set; } = [];
}

public sealed class WorkFromHomeBatchItemResultDto
{
    public DateTime WorkDate { get; set; }
    public WorkLocationType WorkLocation { get; set; }
    public DayEntryType EntryType { get; set; }
    public decimal? SpecificHours { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public WorkFromHomeDto? Entry { get; set; }
}
