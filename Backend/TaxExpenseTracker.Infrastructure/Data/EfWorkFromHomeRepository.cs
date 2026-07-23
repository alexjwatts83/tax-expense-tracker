using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.WorkFromHome;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfWorkFromHomeRepository : EfSoftDeleteRepository<WorkFromHomeEntry>, IWorkFromHomeRepository
{
    public EfWorkFromHomeRepository(AppDbContext dbContext)
        : base(dbContext, dbContext.WorkFromHomeEntries)
    {
    }

    public async Task<IReadOnlyList<WorkFromHomeEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
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
}