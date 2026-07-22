namespace TaxExpenseTracker.Application.Expenses;

public sealed record ExpenseSourceDto(Guid Id, string Name, string? Description, DateTime CreatedAt);

public sealed record ExpenseTagDto(Guid Id, string Name, DateTime CreatedAt);

public sealed record ExpenseReadDto(
    Guid Id,
    string Item,
    string Description,
    DateTime Date,
    string Bank,
    decimal Price,
    Guid SourceId,
    ExpenseSourceDto? Source,
    IReadOnlyList<ExpenseTagDto> Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateExpenseCommand(
    string Item,
    string Description,
    DateTime Date,
    string Bank,
    decimal Price,
    Guid SourceId,
    IReadOnlyList<Guid> TagIds);

public sealed record UpdateExpenseCommand(
    string Item,
    string Description,
    DateTime Date,
    string Bank,
    decimal Price,
    Guid SourceId,
    IReadOnlyList<Guid> TagIds);

public sealed record ExpenseFilterQuery(
    DateTime? StartDate,
    DateTime? EndDate,
    string? Bank,
    decimal? Price,
    Guid? SourceId,
    IReadOnlyList<Guid> TagIds);

public sealed record ExpenseTotalByGroup(string Group, decimal Total);

public sealed record ExpenseSummaryDto(
    decimal TotalSpent,
    IReadOnlyList<ExpenseTotalByGroup> ByBank,
    IReadOnlyList<ExpenseTotalByGroup> BySource);
