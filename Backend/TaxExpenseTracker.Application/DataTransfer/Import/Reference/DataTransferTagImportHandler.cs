using TaxExpenseTracker.Application.Tags;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.DataTransfer;

public sealed class DataTransferTagImportHandler
{
    private readonly ITagRepository _tagRepository;
    private readonly TimeProvider _timeProvider;

    public DataTransferTagImportHandler(ITagRepository tagRepository, TimeProvider timeProvider)
    {
        _tagRepository = tagRepository;
        _timeProvider = timeProvider;
    }

    public async Task<DataTransferEntityImportComputation> ImportAsync(
        IReadOnlyList<ReferenceTagImportItemDto> items,
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
                errors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, $"Tag {item.Id}: Name is required."));
                continue;
            }

            var existing = await _tagRepository.GetByIdIncludingDeletedAsync(item.Id, cancellationToken);
            if (existing is null)
            {
                created += 1;

                if (!options.DryRun)
                {
                    var entity = Tag.Create(item.Name, item.Color, _timeProvider);
                    entity.Id = item.Id;
                    await _tagRepository.AddAsync(entity, cancellationToken);
                }

                continue;
            }

            if (options.Mode == DataTransferImportMode.InsertOnly)
            {
                skipped += 1;
                warnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnInsertOnlySkipped, $"Tag {item.Id}: skipped because it already exists in insertOnly mode."));
                continue;
            }

            updated += 1;

            if (!options.DryRun)
            {
                existing.Rename(item.Name);
                existing.SetColor(item.Color);
                if (existing.IsDeleted)
                    existing.Restore(_timeProvider);
            }
        }

        if (options.Mode == DataTransferImportMode.Replace && options.AllowDeletes)
        {
            deleted = await DataTransferReplaceDeleteUtility.SoftDeleteMissingAsync<Tag>(
                items.Select(x => x.Id).ToList(),
                ct => _tagRepository.GetAllIncludingDeletedAsync(ct),
                (id, ct) => _tagRepository.GetByIdIncludingDeletedAsync(id, ct),
                entity => entity.SoftDelete(_timeProvider),
                options.DryRun,
                cancellationToken);

            if (deleted > 0)
                warnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnReplaceSoftDeletedMissing, $"Replace mode: soft-deleted {deleted} tag records not present in payload."));
        }

        if (!options.DryRun && (created > 0 || updated > 0 || deleted > 0))
            await _tagRepository.SaveChangesAsync(cancellationToken);

        return new DataTransferEntityImportComputation("tags", items.Count, created, updated, skipped, warnings, errors);
    }
}
