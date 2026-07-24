using TaxExpenseTracker.Application.WorkLocation;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.DataTransfer;

public sealed class DataTransferWorkLocationImportHandler
{
    private readonly IWorkLocationRepository _workLocationRepository;
    private readonly TimeProvider _timeProvider;

    public DataTransferWorkLocationImportHandler(
        IWorkLocationRepository workLocationRepository,
        TimeProvider timeProvider)
    {
        _workLocationRepository = workLocationRepository;
        _timeProvider = timeProvider;
    }

    public async Task<DataTransferEntityImportComputation> ImportAsync(
        WorkLocationImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var items = payload.WorkLocationEntries ?? [];
        var warnings = new List<DataTransferImportIssue>();
        var errors = new List<DataTransferImportIssue>();
        var created = 0;
        var updated = 0;
        var skipped = 0;

        DataTransferReplaceDeleteUtility.AddReplaceDeleteNotImplementedWarning(options, warnings);

        foreach (var item in items)
        {
            if (item.Id == Guid.Empty)
            {
                errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, "WorkLocationEntry Id is required."));
                continue;
            }

            if (item.WorkDate == default)
            {
                errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, $"WorkLocationEntry {item.Id}: WorkDate is required."));
                continue;
            }

            var existing = await _workLocationRepository.GetByIdIncludingDeletedAsync(item.Id, cancellationToken);
            var duplicateDateExists = await _workLocationRepository.ExistsForDateAsync(item.WorkDate, item.Id, cancellationToken);
            if (duplicateDateExists)
            {
                errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrDuplicateConflict, $"WorkLocationEntry {item.Id}: another entry already exists for {item.WorkDate:yyyy-MM-dd}."));
                continue;
            }

            if (existing is null)
            {
                created += 1;

                if (!options.DryRun)
                {
                    try
                    {
                        var entity = WorkLocationEntry.Create(item.WorkDate, item.EntryType, item.SpecificHours, item.Notes, _timeProvider, item.WorkLocation);
                        entity.Id = item.Id;

                        if (item.IsDeleted == true)
                            entity.SoftDelete(_timeProvider);

                        await _workLocationRepository.AddAsync(entity, cancellationToken);
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException || ex is InvalidOperationException)
                    {
                        created -= 1;
                        errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrValidation, $"WorkLocationEntry {item.Id}: {ex.Message}"));
                    }
                }

                continue;
            }

            if (options.Mode == DataTransferImportMode.InsertOnly)
            {
                skipped += 1;
                warnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnInsertOnlySkipped, $"WorkLocationEntry {item.Id}: skipped because it already exists in insertOnly mode."));
                continue;
            }

            updated += 1;

            if (!options.DryRun)
            {
                try
                {
                    existing.Update(item.WorkDate, item.EntryType, item.SpecificHours, item.Notes, _timeProvider, item.WorkLocation);

                    if (item.IsDeleted == true)
                        existing.SoftDelete(_timeProvider);
                    else if (existing.IsDeleted)
                        existing.Restore(_timeProvider);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException || ex is InvalidOperationException)
                {
                    updated -= 1;
                    errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrValidation, $"WorkLocationEntry {item.Id}: {ex.Message}"));
                }
            }
        }

        if (!options.DryRun && (created > 0 || updated > 0))
            await _workLocationRepository.SaveChangesAsync(cancellationToken);

        return new DataTransferEntityImportComputation("workLocationEntries", items.Count, created, updated, skipped, warnings, errors);
    }
}
