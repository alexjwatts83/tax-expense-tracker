using Microsoft.EntityFrameworkCore;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Infrastructure.Data;

public abstract class EfSoftDeleteRepository<T> : ISoftDeleteRepository<T> where T : class, ISoftDeletableEntity
{
    protected readonly AppDbContext DbContext;
    protected readonly DbSet<T> DbSet;

    protected EfSoftDeleteRepository(AppDbContext dbContext, DbSet<T> dbSet)
    {
        DbContext = dbContext;
        DbSet = dbSet;
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public virtual Task<T?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public virtual Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        return DbSet.AddAsync(entity, cancellationToken).AsTask();
    }

    public virtual Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return DbContext.SaveChangesAsync(cancellationToken);
    }
}