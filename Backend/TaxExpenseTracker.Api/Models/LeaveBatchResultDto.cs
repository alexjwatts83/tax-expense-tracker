using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Api.Models;

public sealed class LeaveBatchResultDto
{
    public int TotalRequested { get; set; }
    public int CreatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public IReadOnlyList<LeaveBatchItemResultDto> Results { get; set; } = [];
}

public sealed class LeaveBatchItemResultDto
{
    public DateTime LeaveDate { get; set; }
    public LeaveType LeaveType { get; set; } = LeaveType.Annual;
    public DayEntryType EntryType { get; set; }
    public decimal? SpecificHours { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public LeaveDto? Entry { get; set; }
}
