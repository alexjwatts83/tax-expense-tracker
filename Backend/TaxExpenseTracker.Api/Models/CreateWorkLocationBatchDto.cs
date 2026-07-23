namespace TaxExpenseTracker.Api.Models;

public sealed class CreateWorkLocationBatchDto
{
    public IReadOnlyList<CreateWorkLocationDto> Items { get; set; } = [];
}
