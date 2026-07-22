using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Trackers;

public interface ITrackerRepository
{
    Task<IReadOnlyList<Tracker>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Tracker?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Tracker tracker, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}