using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Expenses;

public interface IExpenseRepository
{
    Task<IReadOnlyList<TaxExpense>> GetPagedWithDetailsAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<TaxExpense?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaxExpense?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaxExpense?> GetByIdForRestoreAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SourceExistsAsync(Guid sourceId, CancellationToken cancellationToken = default);
    Task<bool> BankExistsAsync(Guid bankId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetExistingTagIdsAsync(IReadOnlyList<Guid> tagIds, CancellationToken cancellationToken = default);
    Task AddAsync(TaxExpense expense, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalSpentAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseTotalByGroup>> GetTotalByBankAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseTotalByGroup>> GetTotalBySourceAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxExpense>> FilterWithDetailsAsync(ExpenseFilterQuery query, CancellationToken cancellationToken = default);
}
