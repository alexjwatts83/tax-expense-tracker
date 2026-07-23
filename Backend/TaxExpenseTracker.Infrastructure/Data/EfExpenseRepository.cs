using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.Expenses;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfExpenseRepository : IExpenseRepository
{
        public async Task<IReadOnlyList<TaxExpense>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.TaxExpenses
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<TaxExpense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.TaxExpenses
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<TaxExpense?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.TaxExpenses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

    private readonly AppDbContext _dbContext;

    public EfExpenseRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TaxExpense>> GetPagedWithDetailsAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TaxExpenses
            .AsNoTracking()
            .Include(x => x.Bank)
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
            .Include(x => x.Bank)
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

    public Task<bool> SourceExistsAsync(Guid sourceId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Trackers.AnyAsync(x => x.Id == sourceId, cancellationToken);
    }

    public Task<bool> BankExistsAsync(Guid bankId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Banks.AnyAsync(x => x.Id == bankId, cancellationToken);
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
            .Include(x => x.Bank)
            .GroupBy(x => x.Bank!.Name)
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
            .Include(x => x.Bank)
            .Include(x => x.Source)
            .Include(x => x.TaxExpenseTags)
                .ThenInclude(x => x.Tag)
            .AsQueryable();

        if (query.Date.HasValue)
        {
            var filterDate = query.Date.Value.Date;
            expenseQuery = expenseQuery.Where(x => x.Date.Date == filterDate);
        }

        if (query.BankId.HasValue)
        {
            expenseQuery = expenseQuery.Where(x => x.BankId == query.BankId.Value);
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
