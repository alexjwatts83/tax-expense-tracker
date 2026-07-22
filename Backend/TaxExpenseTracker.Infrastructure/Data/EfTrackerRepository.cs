using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.Trackers;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfTrackerRepository : ITrackerRepository
{
    private readonly AppDbContext _dbContext;

    public EfTrackerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Tracker>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Trackers
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Tracker?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Trackers.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public Task AddAsync(Tracker tracker, CancellationToken cancellationToken = default)
    {
        return _dbContext.Trackers.AddAsync(tracker, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
