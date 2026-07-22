namespace TaxExpenseTracker.Application.Tags;

public interface ITagService
{
    Task<IReadOnlyList<TagReadDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TagReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TagReadDto> CreateAsync(CreateTagCommand command, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpdateTagCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
