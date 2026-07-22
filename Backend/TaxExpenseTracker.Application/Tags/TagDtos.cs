namespace TaxExpenseTracker.Application.Tags;

public sealed record TagReadDto(Guid Id, string Name, DateTime CreatedAt);

public sealed record CreateTagCommand(string Name);

public sealed record UpdateTagCommand(string Name);
