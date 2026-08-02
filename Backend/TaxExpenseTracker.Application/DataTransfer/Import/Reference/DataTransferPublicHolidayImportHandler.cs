using TaxExpenseTracker.Application.PublicHolidays;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.DataTransfer;

public sealed class DataTransferPublicHolidayImportHandler
{
    private readonly IPublicHolidayRepository _publicHolidayRepository;
    private readonly TimeProvider _timeProvider;

    public DataTransferPublicHolidayImportHandler(IPublicHolidayRepository publicHolidayRepository, TimeProvider timeProvider)
    {
        _publicHolidayRepository = publicHolidayRepository;
        _timeProvider = timeProvider;
    }

    public async Task<DataTransferEntityImportComputation> ImportAsync(
        IReadOnlyList<ReferencePublicHolidayImportItemDto> items,
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
            ? await _publicHolidayRepository.GetAllForUpdateAsync(cancellationToken)
            : await _publicHolidayRepository.GetByIdsForUpdateAsync(items.Select(x => x.Id).ToList(), cancellationToken);
        var existingById = existingItems.ToDictionary(x => x.Id);

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, $"PublicHoliday {item.Id}: Name is required."));
                continue;
            }

            if (item.HolidayDate == default)
            {
                errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, $"PublicHoliday {item.Id}: HolidayDate is required."));
                continue;
            }

            existingById.TryGetValue(item.Id, out var existing);
            if (existing is null)
            {
                created += 1;

                if (!options.DryRun)
                {
                    var entity = PublicHoliday.Create(
                        item.HolidayDate,
                        item.Name,
                        item.Source,
                        true,
                        _timeProvider,
                        item.CanBeWorkedOn);

                    entity.Id = item.Id;
                    await _publicHolidayRepository.AddAsync(entity, cancellationToken);
                }

                continue;
            }

            if (options.Mode == DataTransferImportMode.InsertOnly)
            {
                skipped += 1;
                warnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnInsertOnlySkipped, $"PublicHoliday {item.Id}: skipped because it already exists in insertOnly mode."));
                continue;
            }

            updated += 1;

            if (!options.DryRun)
            {
                existing.HolidayDate = item.HolidayDate.Date;
                existing.Rename(item.Name);
                existing.Source = string.IsNullOrWhiteSpace(item.Source) ? null : item.Source.Trim();
                existing.IsImported = true;
                existing.SetWorkable(item.CanBeWorkedOn);
            }
        }

        if (options.Mode == DataTransferImportMode.Replace && options.AllowDeletes)
        {
            var payloadIdSet = items.Select(x => x.Id).ToHashSet();
            var idsToDelete = existingItems
                .Where(x => !payloadIdSet.Contains(x.Id))
                .Select(x => x.Id)
                .ToList();

            deleted = idsToDelete.Count;
            if (!options.DryRun && deleted > 0)
                await _publicHolidayRepository.RemoveByIdsAsync(idsToDelete, cancellationToken);

            if (deleted > 0)
                warnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnReplaceDeletedMissing, $"Replace mode: deleted {deleted} public holiday records not present in payload."));
        }

        if (!options.DryRun && (created > 0 || updated > 0 || deleted > 0))
            await _publicHolidayRepository.SaveChangesAsync(cancellationToken);

        return new DataTransferEntityImportComputation("publicHolidays", items.Count, created, updated, skipped, warnings, errors);
    }
}
