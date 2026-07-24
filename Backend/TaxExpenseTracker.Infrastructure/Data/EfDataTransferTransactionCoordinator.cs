using TaxExpenseTracker.Application.DataTransfer;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfDataTransferTransactionCoordinator : IDataTransferTransactionCoordinator
{
    private readonly AppDbContext _dbContext;

    public EfDataTransferTransactionCoordinator(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteAsync<T>(
        bool useTransaction,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!useTransaction)
        {
            return await action(cancellationToken);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await action(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
