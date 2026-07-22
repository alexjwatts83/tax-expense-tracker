namespace TaxExpenseTracker.Application.Trackers;

public interface ITrackerService
{
    Task<IReadOnlyList<TrackerReadDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TrackerReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TrackerReadDto> CreateAsync(CreateTrackerCommand command, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpdateTrackerCommand command, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default);
}
