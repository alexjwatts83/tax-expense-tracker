namespace TaxExpenseTracker.Application.Tags;

public sealed record TagReadDto(Guid Id, string Name, string Color, DateTime CreatedAt);

public sealed record CreateTagCommand(string Name, string? Color = null);

public sealed record UpdateTagCommand(string Name, string? Color = null);
