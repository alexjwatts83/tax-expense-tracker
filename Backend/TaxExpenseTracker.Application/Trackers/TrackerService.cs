using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.Trackers;

public sealed class TrackerService : ITrackerService
{
    private readonly ITrackerRepository _trackerRepository;

    public TrackerService(ITrackerRepository trackerRepository)
    {
        _trackerRepository = trackerRepository;
    }

    public async Task<IReadOnlyList<TrackerReadDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var trackers = await _trackerRepository.GetAllAsync(cancellationToken);

        return trackers
            .OrderBy(t => t.Name)
            .Select(t => new TrackerReadDto(t.Id, t.Name, t.Description, t.CreatedAt))
            .ToList();
    }

    public async Task<TrackerReadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tracker = await _trackerRepository.GetByIdAsync(id, cancellationToken);

        return tracker is null
            ? null
            : new TrackerReadDto(tracker.Id, tracker.Name, tracker.Description, tracker.CreatedAt);
    }

    public async Task<TrackerReadDto> CreateAsync(CreateTrackerCommand command, CancellationToken cancellationToken = default)
    {
        var tracker = Tracker.Create(command.Name, command.Description);

        await _trackerRepository.AddAsync(tracker, cancellationToken);
        await _trackerRepository.SaveChangesAsync(cancellationToken);

        return new TrackerReadDto(tracker.Id, tracker.Name, tracker.Description, tracker.CreatedAt);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateTrackerCommand command, CancellationToken cancellationToken = default)
    {
        var tracker = await _trackerRepository.GetByIdAsync(id, cancellationToken);
        if (tracker is null)
        {
            return false;
        }

        tracker.Rename(command.Name, command.Description);
        await _trackerRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tracker = await _trackerRepository.GetByIdAsync(id, cancellationToken);
        if (tracker is null)
        {
            return false;
        }

        tracker.SoftDelete();
        await _trackerRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tracker = await _trackerRepository.GetByIdIncludingDeletedAsync(id, cancellationToken);
        if (tracker is null || !tracker.IsDeleted)
        {
            return false;
        }

        tracker.Restore();
        await _trackerRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
