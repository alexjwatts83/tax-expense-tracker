using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.WorkLocation;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfWorkLocationRepository : EfSoftDeleteRepository<WorkLocationEntry>, IWorkLocationRepository
{
    public EfWorkLocationRepository(AppDbContext dbContext)
        : base(dbContext, dbContext.WorkLocationEntries)
    {
    }

    public async Task<IReadOnlyList<WorkLocationEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().AsQueryable();

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

    public Task<bool> ExistsForDateAsync(DateTime workDate, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        var date = workDate.Date;

        return DbSet.AnyAsync(
            x => x.WorkDate.Date == date && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }
}