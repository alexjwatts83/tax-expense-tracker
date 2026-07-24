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
    Task<decimal> GetTotalSpentAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseTotalByGroup>> GetTotalByBankAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseTotalByGroup>> GetTotalBySourceAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxExpense>> FilterWithDetailsAsync(ExpenseFilterQuery query, CancellationToken cancellationToken = default);
}
