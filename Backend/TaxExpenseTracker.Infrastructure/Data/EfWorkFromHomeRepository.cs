using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.WorkFromHome;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfWorkFromHomeRepository : IWorkFromHomeRepository
{
    private readonly AppDbContext _dbContext;

    public EfWorkFromHomeRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<WorkFromHomeEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkFromHomeEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkFromHomeEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WorkFromHomeEntries.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.WorkDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.WorkDate <= toDate.Value.Date);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<WorkFromHomeEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkFromHomeEntries
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<WorkFromHomeEntry?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkFromHomeEntries
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task AddAsync(WorkFromHomeEntry entry, CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkFromHomeEntries.AddAsync(entry, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}