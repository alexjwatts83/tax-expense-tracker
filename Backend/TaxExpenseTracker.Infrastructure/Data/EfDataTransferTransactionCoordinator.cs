using TaxExpenseTracker.Application.DataTransfer;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfDataTransferTransactionCoordinator : IDataTransferTransactionCoordinator
{
    private readonly AppDbContext _dbContext;

    public EfDataTransferTransactionCoordinator(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DataTransferImportResultDto> ExecuteAsync(
        bool useTransaction,
        Func<CancellationToken, Task<DataTransferImportResultDto>> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!useTransaction)
        {
            return await action(cancellationToken);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var result = await action(cancellationToken);

        if (result.Results.All(x => x.Errors.Count == 0))
            await transaction.CommitAsync(cancellationToken);
        else
            await transaction.RollbackAsync(cancellationToken);

        return result;
    }
}
