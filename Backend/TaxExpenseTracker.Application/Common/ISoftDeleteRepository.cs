using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Common;

public interface ISoftDeleteRepository<T> : IRepository<T> where T : class, ISoftDeletableEntity
{
    Task<IReadOnlyList<T>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);
    Task<T?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<T>> GetAllForUpdateIncludingDeletedAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllIncludingDeletedAsync(cancellationToken);
    }

    async Task<IReadOnlyList<T>> GetByIdsForUpdateIncludingDeletedAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var entities = new List<T>();
        foreach (var id in ids.Distinct())
        {
            var entity = await GetByIdIncludingDeletedAsync(id, cancellationToken);
            if (entity is not null)
                entities.Add(entity);
        }

        return entities;
    }
}