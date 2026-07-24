namespace TaxExpenseTracker.Application.DataTransfer;

public sealed record DataTransferEntityImportComputation(
    string Entity,
    int ReceivedCount,
    int CreatedCount,
    int UpdatedCount,
    int SkippedCount,
    IReadOnlyList<DataTransferImportIssue> Warnings,
    IReadOnlyList<DataTransferImportIssue> Errors);
