using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.Tags;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public sealed class EfTagRepository : ITagRepository
{
    private readonly AppDbContext _dbContext;

    public EfTagRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tags
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        return _dbContext.Tags.AddAsync(tag, cancellationToken).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
