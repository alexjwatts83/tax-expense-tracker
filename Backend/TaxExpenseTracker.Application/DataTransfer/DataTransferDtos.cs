using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.DataTransfer;

public enum DataTransferImportMode
{
    Upsert = 0,
    InsertOnly = 1,
    Replace = 2,
}

public sealed record DataTransferSourceDto(string App, string Environment);

public sealed record ReferenceDataExportEnvelopeDto(
    int SchemaVersion,
    DateTime ExportedAtUtc,
    DataTransferSourceDto Source,
    ReferenceDataExportDataDto Data);

public sealed record ReferenceDataExportDataDto(
    IReadOnlyList<ReferenceTrackerDto> Trackers,
    IReadOnlyList<ReferenceTagDto> Tags,
    IReadOnlyList<ReferenceBankDto> Banks,
    IReadOnlyList<ReferencePublicHolidayDto> PublicHolidays);

public sealed record ReferenceTrackerDto(Guid Id, string Name, string? Description, DateTime CreatedAt);

public sealed record ReferenceTagDto(Guid Id, string Name, string Color, DateTime CreatedAt);

public sealed record ReferenceBankDto(Guid Id, string Name, DateTime CreatedAt);

public sealed record ReferencePublicHolidayDto(
    Guid Id,
    DateTime HolidayDate,
    string Name,
    string? Source,
    bool IsImported,
    bool CanBeWorkedOn,
    DateTime CreatedAt);

public sealed record ReferenceDataImportPayloadDto(ReferenceDataImportDataDto Data);

public sealed record ReferenceDataImportDataDto(
    IReadOnlyList<ReferenceTrackerImportItemDto>? Trackers,
    IReadOnlyList<ReferenceTagImportItemDto>? Tags,
    IReadOnlyList<ReferenceBankImportItemDto>? Banks,
    IReadOnlyList<ReferencePublicHolidayImportItemDto>? PublicHolidays);

public sealed record ReferenceTrackerImportItemDto(Guid Id, string Name, string? Description);

public sealed record ReferenceTagImportItemDto(Guid Id, string Name, string Color);

public sealed record ReferenceBankImportItemDto(Guid Id, string Name);

public sealed record ReferencePublicHolidayImportItemDto(
    Guid Id,
    DateTime HolidayDate,
    string Name,
    string? Source,
    bool CanBeWorkedOn);

public sealed record ExpenseImportPayloadDto(
    IReadOnlyList<ExpenseImportItemDto>? Expenses,
    IReadOnlyList<ExpenseTagImportItemDto>? ExpenseTags);

public sealed record ExpenseImportItemDto(
    Guid Id,
    DateTime Date,
    string Description,
    decimal Price,
    Guid BankId,
    Guid SourceId,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    bool? IsDeleted);

public sealed record ExpenseTagImportItemDto(Guid Id, Guid TaxExpenseId, Guid TagId);

public sealed record WorkLocationImportPayloadDto(IReadOnlyList<WorkLocationEntryImportItemDto>? WorkLocationEntries);

public sealed record WorkLocationEntryImportItemDto(
    Guid Id,
    DateTime WorkDate,
    DayEntryType EntryType,
    decimal? SpecificHours,
    string? Notes,
    WorkLocationType WorkLocation,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    bool? IsDeleted);

public sealed record LeaveImportPayloadDto(IReadOnlyList<LeaveEntryImportItemDto>? LeaveEntries);

public sealed record LeaveEntryImportItemDto(
    Guid Id,
    DateTime LeaveDate,
    DayEntryType EntryType,
    decimal? SpecificHours,
    string? Notes,
    LeaveType LeaveType,
    DateTime? CreatedAt,
    DateTime? UpdatedAt,
    bool? IsDeleted);

public sealed record DataTransferImportOptions(
    DataTransferImportMode Mode = DataTransferImportMode.Upsert,
    bool DryRun = false,
    bool AllowDeletes = false);

public sealed record DataTransferEntityImportResultDto(
    string Entity,
    int ReceivedCount,
    int CreatedCount,
    int UpdatedCount,
    int SkippedCount,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> WarningCodes,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> ErrorCodes);

public sealed record DataTransferImportResultDto(
    bool DryRun,
    DataTransferImportMode Mode,
    IReadOnlyList<DataTransferEntityImportResultDto> Results,
    string? CorrelationId = null);
