using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Api.Models;

public sealed class WorkLocationDto
{
    public Guid Id { get; set; }
    public DateTime WorkDate { get; set; }
    public WorkLocationType WorkLocation { get; set; }
    public DayEntryType EntryType { get; set; }
    public decimal HoursWorked { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}