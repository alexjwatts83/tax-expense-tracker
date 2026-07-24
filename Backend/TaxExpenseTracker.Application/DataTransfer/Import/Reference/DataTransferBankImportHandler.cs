using TaxExpenseTracker.Application.Banks;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.DataTransfer;

public sealed class DataTransferBankImportHandler
{
    private readonly IBankRepository _bankRepository;
    private readonly TimeProvider _timeProvider;

    public DataTransferBankImportHandler(IBankRepository bankRepository, TimeProvider timeProvider)
    {
        _bankRepository = bankRepository;
        _timeProvider = timeProvider;
    }

    public async Task<DataTransferEntityImportComputation> ImportAsync(
        IReadOnlyList<ReferenceBankImportItemDto> items,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var warnings = new List<DataTransferImportIssue>();
        var errors = new List<DataTransferImportIssue>();
        var deleted = 0;

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, $"Bank {item.Id}: Name is required."));
                continue;
            }

            var existing = await _bankRepository.GetByIdIncludingDeletedAsync(item.Id, cancellationToken);
            if (existing is null)
            {
                created += 1;

                if (!options.DryRun)
                {
                    var entity = Bank.Create(item.Name, _timeProvider);
                    entity.Id = item.Id;
                    await _bankRepository.AddAsync(entity, cancellationToken);
                }

                continue;
            }

            if (options.Mode == DataTransferImportMode.InsertOnly)
            {
                skipped += 1;
                warnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnInsertOnlySkipped, $"Bank {item.Id}: skipped because it already exists in insertOnly mode."));
                continue;
            }

            updated += 1;

            if (!options.DryRun)
            {
                existing.Rename(item.Name);
                if (existing.IsDeleted)
                    existing.Restore(_timeProvider);
            }
        }

        if (options.Mode == DataTransferImportMode.Replace && options.AllowDeletes)
        {
            deleted = await DataTransferReplaceDeleteUtility.SoftDeleteMissingAsync<Bank>(
                items.Select(x => x.Id).ToList(),
                ct => _bankRepository.GetAllIncludingDeletedAsync(ct),
                (id, ct) => _bankRepository.GetByIdIncludingDeletedAsync(id, ct),
                entity => entity.SoftDelete(_timeProvider),
                options.DryRun,
                cancellationToken);

            if (deleted > 0)
                warnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnReplaceSoftDeletedMissing, $"Replace mode: soft-deleted {deleted} bank records not present in payload."));
        }

        if (!options.DryRun && (created > 0 || updated > 0 || deleted > 0))
            await _bankRepository.SaveChangesAsync(cancellationToken);

        return new DataTransferEntityImportComputation("banks", items.Count, created, updated, skipped, warnings, errors);
    }
}
