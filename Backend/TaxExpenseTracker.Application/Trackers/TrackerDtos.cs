namespace TaxExpenseTracker.Application.Trackers;

public sealed record TrackerReadDto(Guid Id, string Name, string? Description, DateTime CreatedAt);

public sealed record CreateTrackerCommand(string Name, string? Description);

public sealed record UpdateTrackerCommand(string Name, string? Description);
