using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.PublicHolidays;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfPublicHolidayRepository : IPublicHolidayRepository
{
    private readonly AppDbContext _dbContext;

    public EfPublicHolidayRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PublicHoliday>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PublicHolidays
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<PublicHoliday?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PublicHolidays
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PublicHoliday>> GetByDateRangeAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PublicHolidays
            .AsNoTracking()
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.HolidayDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.HolidayDate <= toDate.Value.Date);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task AddAsync(PublicHoliday entity, CancellationToken cancellationToken = default)
    {
        _dbContext.PublicHolidays.Add(entity);
        return Task.CompletedTask;
    }

    public async Task RemoveByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return;
        }

        var entities = await _dbContext.PublicHolidays
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);

        _dbContext.PublicHolidays.RemoveRange(entities);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
