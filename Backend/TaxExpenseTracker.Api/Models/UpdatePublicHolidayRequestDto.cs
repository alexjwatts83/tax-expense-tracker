namespace TaxExpenseTracker.Api.Models;

public sealed class UpdatePublicHolidayRequestDto
{
    public DateTime HolidayDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Source { get; set; }
    public bool CanBeWorkedOn { get; set; }
}
