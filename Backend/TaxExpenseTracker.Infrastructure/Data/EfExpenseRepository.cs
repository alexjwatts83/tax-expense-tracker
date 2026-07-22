using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.Expenses;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfExpenseRepository : IExpenseRepository
{
    private readonly AppDbContext _dbContext;

    public EfExpenseRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TaxExpense>> GetPagedWithDetailsAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaxExpenses
            .AsNoTracking()
            .Include(x => x.Source)
            .Include(x => x.TaxExpenseTags)
                .ThenInclude(x => x.Tag)
            .OrderByDescending(x => x.Date)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaxExpense?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaxExpenses
            .AsNoTracking()
            .Include(x => x.Source)
            .Include(x => x.TaxExpenseTags)
                .ThenInclude(x => x.Tag)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<TaxExpense?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaxExpenses
            .Include(x => x.TaxExpenseTags)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<TaxExpense?> GetByIdForRestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaxExpenses
            .IgnoreQueryFilters()
            .Include(x => x.TaxExpenseTags)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> SourceExistsAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Trackers.AnyAsync(x => x.Id == sourceId, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetExistingTagIdsAsync(IReadOnlyList<Guid> tagIds, CancellationToken cancellationToken = default)
    {
        if (tagIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Tags
            .Where(x => tagIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(TaxExpense expense, CancellationToken cancellationToken = default)
    {
        return _dbContext.TaxExpenses.AddAsync(expense, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<decimal> GetTotalSpentAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.TaxExpenses.SumAsync(x => x.Price, cancellationToken);
    }

    public async Task<IReadOnlyList<ExpenseTotalByGroup>> GetTotalByBankAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.TaxExpenses
            .GroupBy(x => x.Bank)
            .Select(x => new { Group = x.Key, Total = x.Sum(e => e.Price) })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new ExpenseTotalByGroup(x.Group, x.Total)).ToList();
    }

    public async Task<IReadOnlyList<ExpenseTotalByGroup>> GetTotalBySourceAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.TaxExpenses
            .Include(x => x.Source)
            .GroupBy(x => x.Source!.Name)
            .Select(x => new { Group = x.Key, Total = x.Sum(e => e.Price) })
            .OrderByDescending(x => x.Total)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new ExpenseTotalByGroup(x.Group, x.Total)).ToList();
    }

    public async Task<IReadOnlyList<TaxExpense>> FilterWithDetailsAsync(ExpenseFilterQuery query, CancellationToken cancellationToken = default)
    {
        var expenseQuery = _dbContext.TaxExpenses
            .AsNoTracking()
            .Include(x => x.Source)
            .Include(x => x.TaxExpenseTags)
                .ThenInclude(x => x.Tag)
            .AsQueryable();

        if (query.StartDate.HasValue)
        {
            expenseQuery = expenseQuery.Where(x => x.Date >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            expenseQuery = expenseQuery.Where(x => x.Date <= query.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Bank))
        {
            var trimmedBank = query.Bank.Trim();
            expenseQuery = expenseQuery.Where(x => x.Bank == trimmedBank);
        }

        if (query.Price.HasValue)
        {
            expenseQuery = expenseQuery.Where(x => x.Price <= query.Price.Value);
        }

        if (query.SourceId.HasValue)
        {
            expenseQuery = expenseQuery.Where(x => x.SourceId == query.SourceId.Value);
        }

        if (query.TagIds.Count > 0)
        {
            expenseQuery = expenseQuery.Where(x => x.TaxExpenseTags.Any(tag => query.TagIds.Contains(tag.TagId)));
        }

        return await expenseQuery
            .OrderByDescending(x => x.Date)
            .ToListAsync(cancellationToken);
    }
}
