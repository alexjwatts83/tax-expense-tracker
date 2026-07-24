namespace TaxExpenseTracker.Application.DataTransfer;

public interface IDataTransferService
{
    Task<ReferenceDataExportEnvelopeDto> ExportReferenceDataAsync(bool includeSoftDeleted, CancellationToken cancellationToken = default);
    Task<ExpenseImportPayloadDto> ExportExpensesAsync(bool includeSoftDeleted, CancellationToken cancellationToken = default);
    Task<WorkLocationImportPayloadDto> ExportWorkLocationsAsync(bool includeSoftDeleted, CancellationToken cancellationToken = default);
    Task<LeaveImportPayloadDto> ExportLeaveAsync(bool includeSoftDeleted, CancellationToken cancellationToken = default);

    Task<DataTransferImportResultDto> ImportReferenceDataAsync(
        ReferenceDataImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default);

    Task<DataTransferImportResultDto> ImportExpensesAsync(
        ExpenseImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default);

    Task<DataTransferImportResultDto> ImportWorkLocationsAsync(
        WorkLocationImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default);

    Task<DataTransferImportResultDto> ImportLeaveAsync(
        LeaveImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default);
}
