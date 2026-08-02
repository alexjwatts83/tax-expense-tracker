using TaxExpenseTracker.Application.Trackers;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.DataTransfer;

public sealed class DataTransferTrackerImportHandler
{
    private readonly ITrackerRepository _trackerRepository;
    private readonly TimeProvider _timeProvider;

    public DataTransferTrackerImportHandler(ITrackerRepository trackerRepository, TimeProvider timeProvider)
    {
        _trackerRepository = trackerRepository;
        _timeProvider = timeProvider;
    }

    public async Task<DataTransferEntityImportComputation> ImportAsync(
        IReadOnlyList<ReferenceTrackerImportItemDto> items,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var warnings = new List<DataTransferImportIssue>();
        var errors = new List<DataTransferImportIssue>();
        var deleted = 0;
        var existingItems = options.Mode == DataTransferImportMode.Replace && options.AllowDeletes
            ? await _trackerRepository.GetAllForUpdateIncludingDeletedAsync(cancellationToken)
            : await _trackerRepository.GetByIdsForUpdateIncludingDeletedAsync(items.Select(x => x.Id).ToList(), cancellationToken);
        var existingById = existingItems.ToDictionary(x => x.Id);

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, $"Tracker {item.Id}: Name is required."));
                continue;
            }

            existingById.TryGetValue(item.Id, out var existing);
            if (existing is null)
            {
                created += 1;

                if (!options.DryRun)
                {
                    var entity = Tracker.Create(item.Name, item.Description, _timeProvider);
                    entity.Id = item.Id;
                    await _trackerRepository.AddAsync(entity, cancellationToken);
                }

                continue;
            }

            if (options.Mode == DataTransferImportMode.InsertOnly)
            {
                skipped += 1;
                warnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnInsertOnlySkipped, $"Tracker {item.Id}: skipped because it already exists in insertOnly mode."));
                continue;
            }

            updated += 1;

            if (!options.DryRun)
            {
                existing.Rename(item.Name, item.Description, _timeProvider);
                if (existing.IsDeleted)
                    existing.Restore(_timeProvider);
            }
        }

        if (options.Mode == DataTransferImportMode.Replace && options.AllowDeletes)
        {
            deleted = DataTransferReplaceDeleteUtility.SoftDeleteMissing<Tracker>(
                items.Select(x => x.Id).ToList(),
                existingItems,
                entity => entity.SoftDelete(_timeProvider),
                options.DryRun);

            if (deleted > 0)
                warnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnReplaceSoftDeletedMissing, $"Replace mode: soft-deleted {deleted} tracker records not present in payload."));
        }

        if (!options.DryRun && (created > 0 || updated > 0 || deleted > 0))
            await _trackerRepository.SaveChangesAsync(cancellationToken);

        return new DataTransferEntityImportComputation("trackers", items.Count, created, updated, skipped, warnings, errors);
    }
}
