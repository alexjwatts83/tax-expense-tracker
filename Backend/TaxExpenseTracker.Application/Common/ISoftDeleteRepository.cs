using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Common;

public interface ISoftDeleteRepository<T> : IRepository<T> where T : class, ISoftDeletableEntity
{
    Task<T?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
}