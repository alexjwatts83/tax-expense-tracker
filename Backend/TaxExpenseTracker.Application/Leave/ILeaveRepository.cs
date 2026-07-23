using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Leave;

public interface ILeaveRepository
{
    Task<IReadOnlyList<LeaveEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<LeaveEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LeaveEntry?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(LeaveEntry entry, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}