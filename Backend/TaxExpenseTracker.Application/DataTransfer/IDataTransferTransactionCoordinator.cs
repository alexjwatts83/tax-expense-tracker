namespace TaxExpenseTracker.Application.DataTransfer;

public interface IDataTransferTransactionCoordinator
{
    Task<T> ExecuteAsync<T>(
        bool useTransaction,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
