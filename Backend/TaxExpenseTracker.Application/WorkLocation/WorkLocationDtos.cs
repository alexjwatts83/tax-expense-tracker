using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.WorkLocation;

public sealed record WorkLocationReadDto(
    Guid Id,
    DateTime WorkDate,
    DayEntryType EntryType,
    decimal HoursWorked,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    WorkLocationType WorkLocation = WorkLocationType.Wfh);

public sealed record CreateWorkLocationCommand(
    DateTime WorkDate,
    DayEntryType EntryType,
    decimal? SpecificHours,
    string? Notes,
    WorkLocationType WorkLocation = WorkLocationType.Wfh);

public sealed record UpdateWorkLocationCommand(
    DateTime WorkDate,
    DayEntryType EntryType,
    decimal? SpecificHours,
    string? Notes,
    WorkLocationType WorkLocation = WorkLocationType.Wfh);

public sealed record BatchCreateWorkLocationResult(
    int TotalRequested,
    int CreatedCount,
    int SkippedCount,
    int FailedCount,
    IReadOnlyList<BatchCreateWorkLocationItemResult> Results);

public sealed record BatchCreateWorkLocationItemResult(
    DateTime WorkDate,
    WorkLocationType WorkLocation,
    DayEntryType EntryType,
    decimal? SpecificHours,
    string? Notes,
    string Status,
    string? Message,
    WorkLocationReadDto? Entry);