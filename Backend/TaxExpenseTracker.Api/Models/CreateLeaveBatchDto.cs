namespace TaxExpenseTracker.Api.Models;

public sealed class CreateLeaveBatchDto
{
    public IReadOnlyList<CreateLeaveDto> Items { get; set; } = [];
}
