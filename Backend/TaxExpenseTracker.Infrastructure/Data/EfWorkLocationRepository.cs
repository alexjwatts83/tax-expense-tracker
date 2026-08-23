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

    public async Task<IReadOnlyList<WorkLocationEntry>> GetByDateRangeAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.WorkDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.WorkDate <= toDate.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsForDateAsync(DateOnly workDate, Guid? excludingId = null, CancellationToken cancellationToken = default)
    {
        return DbSet.AnyAsync(
            x => x.WorkDate == workDate && (!excludingId.HasValue || x.Id != excludingId.Value),
            cancellationToken);
    }

    public async Task<IReadOnlyList<WorkLocationEntry>> GetByDatesAsync(
        IReadOnlyCollection<DateOnly> workDates,
        CancellationToken cancellationToken = default)
    {
        if (workDates.Count == 0)
            return [];

        var dates = workDates.Distinct().ToList();
        return await DbSet
            .AsNoTracking()
            .Where(x => dates.Contains(x.WorkDate))
            .ToListAsync(cancellationToken);
    }
}