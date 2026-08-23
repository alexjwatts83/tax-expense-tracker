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

    public async Task<IReadOnlyList<LeaveEntry>> GetByDateRangeAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.LeaveDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.LeaveDate <= toDate.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsForDateAsync(DateOnly leaveDate, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            x => x.LeaveDate == leaveDate && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveEntry>> GetByDatesAsync(
        IReadOnlyCollection<DateOnly> leaveDates,
        CancellationToken cancellationToken = default)
    {
        if (leaveDates.Count == 0)
            return [];

        var dates = leaveDates.Distinct().ToList();
        return await DbSet
            .AsNoTracking()
            .Where(x => dates.Contains(x.LeaveDate))
            .ToListAsync(cancellationToken);
    }
}