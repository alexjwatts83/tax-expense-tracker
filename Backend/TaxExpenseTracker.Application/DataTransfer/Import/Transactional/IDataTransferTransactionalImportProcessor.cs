namespace TaxExpenseTracker.Application.DataTransfer;

public interface IDataTransferTransactionalImportProcessor
{
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
