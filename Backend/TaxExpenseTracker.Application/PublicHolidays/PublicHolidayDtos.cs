namespace TaxExpenseTracker.Application.PublicHolidays;

public sealed record PublicHolidayReadDto(
    Guid Id,
    DateOnly HolidayDate,
    string Name,
    string? Source,
    bool IsImported,
    bool CanBeWorkedOn,
    DateTime CreatedAt);

public sealed record PublicHolidayImportResultDto(
    int ImportedCount,
    int SkippedDuplicateCount,
    IReadOnlyList<string> Warnings);

public sealed record UpdatePublicHolidayCommand(
    DateOnly HolidayDate,
    string Name,
    string? Source,
    bool CanBeWorkedOn);
