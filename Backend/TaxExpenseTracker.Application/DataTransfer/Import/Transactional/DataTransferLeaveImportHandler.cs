using TaxExpenseTracker.Application.Leave;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.DataTransfer;

public sealed class DataTransferLeaveImportHandler
{
    private readonly ILeaveRepository _leaveRepository;
    private readonly TimeProvider _timeProvider;

    public DataTransferLeaveImportHandler(
        ILeaveRepository leaveRepository,
        TimeProvider timeProvider)
    {
        _leaveRepository = leaveRepository;
        _timeProvider = timeProvider;
    }

    public async Task<DataTransferEntityImportComputation> ImportAsync(
        LeaveImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var items = payload.LeaveEntries ?? [];
        var warnings = new List<DataTransferImportIssue>();
        var errors = new List<DataTransferImportIssue>();
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var deleted = 0;
        var existingItems = options.Mode == DataTransferImportMode.Replace && options.AllowDeletes
            ? await _leaveRepository.GetAllForUpdateIncludingDeletedAsync(cancellationToken)
            : await _leaveRepository.GetByIdsForUpdateIncludingDeletedAsync(items.Select(x => x.Id).ToList(), cancellationToken);
        var existingById = existingItems.ToDictionary(x => x.Id);
        var entriesOnRequestedDates = await _leaveRepository.GetByDatesAsync(items.Select(x => x.LeaveDate).ToList(), cancellationToken);
        var existingIdByDate = entriesOnRequestedDates.ToDictionary(x => x.LeaveDate.Date, x => x.Id);
        var acceptedIdByDate = new Dictionary<DateTime, Guid>();

        foreach (var item in items)
        {
            if (item.Id == Guid.Empty)
            {
                errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, "LeaveEntry Id is required."));
                continue;
            }

            if (item.LeaveDate == default)
            {
                errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, $"LeaveEntry {item.Id}: LeaveDate is required."));
                continue;
            }

            existingById.TryGetValue(item.Id, out var existing);
            var leaveDate = item.LeaveDate.Date;
            var conflictsWithExisting = existingIdByDate.TryGetValue(leaveDate, out var existingId) && existingId != item.Id;
            var conflictsWithinPayload = acceptedIdByDate.TryGetValue(leaveDate, out var acceptedId) && acceptedId != item.Id;
            if (conflictsWithExisting || conflictsWithinPayload)
            {
                errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrDuplicateConflict, $"LeaveEntry {item.Id}: another entry already exists for {item.LeaveDate:yyyy-MM-dd}."));
                continue;
            }

            acceptedIdByDate[leaveDate] = item.Id;

            if (existing is null)
            {
                created += 1;

                if (!options.DryRun)
                {
                    try
                    {
                        var entity = LeaveEntry.Create(item.LeaveDate, item.EntryType, item.SpecificHours, item.Notes, _timeProvider, item.LeaveType);
                        entity.Id = item.Id;

                        if (item.IsDeleted == true)
                            entity.SoftDelete(_timeProvider);

                        await _leaveRepository.AddAsync(entity, cancellationToken);
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException || ex is InvalidOperationException)
                    {
                        created -= 1;
                        errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrValidation, $"LeaveEntry {item.Id}: {ex.Message}"));
                    }
                }

                continue;
            }

            if (options.Mode == DataTransferImportMode.InsertOnly)
            {
                skipped += 1;
                warnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnInsertOnlySkipped, $"LeaveEntry {item.Id}: skipped because it already exists in insertOnly mode."));
                continue;
            }

            updated += 1;

            if (!options.DryRun)
            {
                try
                {
                    existing.Update(item.LeaveDate, item.EntryType, item.SpecificHours, item.Notes, _timeProvider, item.LeaveType);

                    if (item.IsDeleted == true)
                        existing.SoftDelete(_timeProvider);
                    else if (existing.IsDeleted)
                        existing.Restore(_timeProvider);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException || ex is InvalidOperationException)
                {
                    updated -= 1;
                    errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrValidation, $"LeaveEntry {item.Id}: {ex.Message}"));
                }
            }
        }

        if (options.Mode == DataTransferImportMode.Replace && options.AllowDeletes)
        {
            deleted = DataTransferReplaceDeleteUtility.SoftDeleteMissing<LeaveEntry>(
                items.Select(x => x.Id).ToList(),
                existingItems,
                entity => entity.SoftDelete(_timeProvider),
                options.DryRun);

            if (deleted > 0)
                warnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnReplaceSoftDeletedMissing, $"Replace mode: soft-deleted {deleted} leave records not present in payload."));
        }

        if (!options.DryRun && (created > 0 || updated > 0 || deleted > 0))
            await _leaveRepository.SaveChangesAsync(cancellationToken);

        return new DataTransferEntityImportComputation("leaveEntries", items.Count, created, updated, skipped, warnings, errors);
    }
}
