using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Api.Models;

public sealed class LeaveDto
{
    public Guid Id { get; set; }
    public DateTime LeaveDate { get; set; }
    public DayEntryType EntryType { get; set; }
    public decimal HoursWorked { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}