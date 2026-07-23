using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Leave;

public sealed record LeaveReadDto(
    Guid Id,
    DateTime LeaveDate,
    DayEntryType EntryType,
    decimal HoursWorked,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateLeaveCommand(
    DateTime LeaveDate,
    DayEntryType EntryType,
    decimal? SpecificHours,
    string? Notes);

public sealed record UpdateLeaveCommand(
    DateTime LeaveDate,
    DayEntryType EntryType,
    decimal? SpecificHours,
    string? Notes);