namespace TaxExpenseTracker.Application.PublicHolidays;

public sealed record PublicHolidayReadDto(
    Guid Id,
    DateTime HolidayDate,
    string Name,
    string? Source,
    bool IsImported,
    DateTime CreatedAt);

public sealed record PublicHolidayImportResultDto(
    int ImportedCount,
    int SkippedDuplicateCount,
    IReadOnlyList<string> Warnings);
