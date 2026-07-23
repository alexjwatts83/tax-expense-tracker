namespace TaxExpenseTracker.Api.Models;

public sealed class CreateWorkFromHomeBatchDto
{
    public IReadOnlyList<CreateWorkFromHomeDto> Items { get; set; } = [];
}
