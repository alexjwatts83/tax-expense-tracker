using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.Banks;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfBankRepository : IBankRepository
{
    private readonly AppDbContext _dbContext;

    public EfBankRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Bank>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Banks
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Bank?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Banks.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Bank?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Banks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public Task AddAsync(Bank bank, CancellationToken cancellationToken = default)
    {
        return _dbContext.Banks.AddAsync(bank, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}