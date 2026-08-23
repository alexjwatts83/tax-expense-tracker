namespace TaxExpenseTracker.Api.Models;

public sealed class PublicHolidayDto
{
    public Guid Id { get; set; }
    public DateOnly HolidayDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Source { get; set; }
    public bool IsImported { get; set; }
    public bool CanBeWorkedOn { get; set; }
    public DateTime CreatedAt { get; set; }
}
