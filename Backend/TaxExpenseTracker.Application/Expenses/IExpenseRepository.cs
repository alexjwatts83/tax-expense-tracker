using TaxExpenseTracker.Domain.Entities;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Application.Expenses;

public interface IExpenseRepository : ISoftDeleteRepository<TaxExpense>
{
    Task<IReadOnlyList<TaxExpense>> GetAllForExportAsync(bool includeSoftDeleted, CancellationToken cancellationToken = default);
    Task<PagedResult<TaxExpense>> GetPagedWithDetailsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<TaxExpense?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaxExpense?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaxExpense?> GetByIdForUpdateIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SourceExistsAsync(Guid sourceId, CancellationToken cancellationToken = default);
    Task<bool> BankExistsAsync(Guid bankId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetExistingTagIdsAsync(IReadOnlyList<Guid> tagIds, CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<Guid>> GetExistingSourceIdsAsync(
        IReadOnlyCollection<Guid> sourceIds,
        CancellationToken cancellationToken = default)
    {
        var existingIds = new List<Guid>();
        foreach (var sourceId in sourceIds.Distinct())
        {
            if (await SourceExistsAsync(sourceId, cancellationToken))
                existingIds.Add(sourceId);
        }

        return existingIds;
    }

    async Task<IReadOnlyList<Guid>> GetExistingBankIdsAsync(
        IReadOnlyCollection<Guid> bankIds,
        CancellationToken cancellationToken = default)
    {
        var existingIds = new List<Guid>();
        foreach (var bankId in bankIds.Distinct())
        {
            if (await BankExistsAsync(bankId, cancellationToken))
                existingIds.Add(bankId);
        }

        return existingIds;
    }
    Task<decimal> GetTotalSpentAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseTotalByGroup>> GetTotalByBankAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseTotalByGroup>> GetTotalBySourceAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxExpense>> FilterWithDetailsAsync(ExpenseFilterQuery query, CancellationToken cancellationToken = default);
}
