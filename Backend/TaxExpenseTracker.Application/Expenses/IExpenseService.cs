namespace TaxExpenseTracker.Application.Expenses;

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseReadDto>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ExpenseReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExpenseReadDto> CreateAsync(CreateExpenseCommand command, CancellationToken cancellationToken = default);
    Task<ExpenseReadDto?> UpdateAsync(Guid id, UpdateExpenseCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExpenseSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseReadDto>> FilterAsync(ExpenseFilterQuery query, CancellationToken cancellationToken = default);
}
