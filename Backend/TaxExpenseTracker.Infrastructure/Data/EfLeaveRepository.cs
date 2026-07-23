using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.Leave;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfLeaveRepository : EfSoftDeleteRepository<LeaveEntry>, ILeaveRepository
{
    public EfLeaveRepository(AppDbContext dbContext)
        : base(dbContext, dbContext.LeaveEntries)
    {
    }

    public async Task<IReadOnlyList<LeaveEntry>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.LeaveDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.LeaveDate <= toDate.Value.Date);
        }

        return await query.ToListAsync(cancellationToken);
    }
}