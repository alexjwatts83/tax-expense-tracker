using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Tags;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tag?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Tag tag, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
