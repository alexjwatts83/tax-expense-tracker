using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.WorkFromHome;

public sealed record WorkFromHomeReadDto(
    Guid Id,
    DateTime WorkDate,
    DayEntryType EntryType,
    decimal HoursWorked,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateWorkFromHomeCommand(
    DateTime WorkDate,
    DayEntryType EntryType,
    decimal? SpecificHours,
    string? Notes);

public sealed record UpdateWorkFromHomeCommand(
    DateTime WorkDate,
    DayEntryType EntryType,
    decimal? SpecificHours,
    string? Notes);

public sealed record BatchCreateWorkFromHomeResult(
    int TotalRequested,
    int CreatedCount,
    int SkippedCount,
    int FailedCount,
    IReadOnlyList<BatchCreateWorkFromHomeItemResult> Results);

public sealed record BatchCreateWorkFromHomeItemResult(
    DateTime WorkDate,
    DayEntryType EntryType,
    decimal? SpecificHours,
    string? Notes,
    string Status,
    string? Message,
    WorkFromHomeReadDto? Entry);