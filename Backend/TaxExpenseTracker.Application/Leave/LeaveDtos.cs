using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Leave;

public sealed record LeaveReadDto(
    Guid Id,
    DateOnly LeaveDate,
    LeaveType LeaveType,
    DayEntryType EntryType,
    decimal HoursWorked,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateLeaveCommand(
    DateOnly LeaveDate,
    DayEntryType EntryType,
    decimal? SpecificHours,
    string? Notes,
    LeaveType LeaveType = LeaveType.Annual);

public sealed record UpdateLeaveCommand(
    DateOnly LeaveDate,
    DayEntryType EntryType,
    decimal? SpecificHours,
    string? Notes,
    LeaveType LeaveType = LeaveType.Annual);

public sealed record BatchCreateLeaveResult(
    int TotalRequested,
    int CreatedCount,
    int SkippedCount,
    int FailedCount,
    IReadOnlyList<BatchCreateLeaveItemResult> Results);

public sealed record BatchCreateLeaveItemResult(
    DateOnly LeaveDate,
    DayEntryType EntryType,
    decimal? SpecificHours,
    string? Notes,
    LeaveType LeaveType,
    string Status,
    string? Message,
    LeaveReadDto? Entry);