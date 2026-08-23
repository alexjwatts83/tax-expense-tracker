using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Api.Models;

public sealed class CreateWorkLocationDto
{
    public DateOnly WorkDate { get; set; }
    public WorkLocationType WorkLocation { get; set; } = WorkLocationType.Wfh;
    public DayEntryType EntryType { get; set; }
    public decimal? SpecificHours { get; set; }
    public string? Notes { get; set; }
}