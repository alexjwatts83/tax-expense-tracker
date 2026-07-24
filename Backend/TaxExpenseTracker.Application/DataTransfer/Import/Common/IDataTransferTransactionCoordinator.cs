namespace TaxExpenseTracker.Application.DataTransfer;

public interface IDataTransferTransactionCoordinator
{
    Task<DataTransferImportResultDto> ExecuteAsync(
        bool useTransaction,
        Func<CancellationToken, Task<DataTransferImportResultDto>> action,
        CancellationToken cancellationToken = default);
}
