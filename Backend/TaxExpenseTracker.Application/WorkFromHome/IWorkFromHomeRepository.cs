using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.WorkFromHome;

public interface IWorkFromHomeRepository
{
    Task<IReadOnlyList<WorkFromHomeEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkFromHomeEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<WorkFromHomeEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkFromHomeEntry?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(WorkFromHomeEntry entry, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}