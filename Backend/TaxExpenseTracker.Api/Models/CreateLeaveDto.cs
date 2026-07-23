using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Api.Models;

public sealed class CreateLeaveDto
{
    public DateTime LeaveDate { get; set; }
    public LeaveType LeaveType { get; set; } = LeaveType.Annual;
    public DayEntryType EntryType { get; set; }
    public decimal? SpecificHours { get; set; }
    public string? Notes { get; set; }
}