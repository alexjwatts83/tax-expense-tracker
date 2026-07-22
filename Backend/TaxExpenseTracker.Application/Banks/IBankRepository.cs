using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Banks;

public interface IBankRepository
{
    Task<IReadOnlyList<Bank>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Bank?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Bank?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Bank bank, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}