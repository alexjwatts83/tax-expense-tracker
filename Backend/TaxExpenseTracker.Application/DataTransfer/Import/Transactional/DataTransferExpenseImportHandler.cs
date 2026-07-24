using TaxExpenseTracker.Application.Expenses;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.DataTransfer;

public sealed class DataTransferExpenseImportHandler
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly TimeProvider _timeProvider;

    public DataTransferExpenseImportHandler(IExpenseRepository expenseRepository, TimeProvider timeProvider)
    {
        _expenseRepository = expenseRepository;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<DataTransferEntityImportComputation>> ImportAsync(
        ExpenseImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var expenses = payload.Expenses ?? [];
        var expenseTags = payload.ExpenseTags ?? [];

        var modeWarnings = new List<DataTransferImportIssue>();
        DataTransferReplaceDeleteUtility.AddReplaceDeleteNotImplementedWarning(options, modeWarnings);

        var expenseWarnings = new List<DataTransferImportIssue>(modeWarnings);
        var expenseErrors = new List<DataTransferImportIssue>();
        var expenseCreated = 0;
        var expenseUpdated = 0;
        var expenseSkipped = 0;

        var trackedExpenses = new Dictionary<Guid, TaxExpense>();
        var acceptedExpenseIds = new HashSet<Guid>();

        foreach (var item in expenses)
        {
            if (item.Id == Guid.Empty)
            {
                expenseErrors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, "Expense Id is required."));
                continue;
            }

            if (item.BankId == Guid.Empty)
            {
                expenseErrors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, $"Expense {item.Id}: BankId is required."));
                continue;
            }

            if (item.SourceId == Guid.Empty)
            {
                expenseErrors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, $"Expense {item.Id}: SourceId is required."));
                continue;
            }

            if (item.Date == default)
            {
                expenseErrors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, $"Expense {item.Id}: Date is required."));
                continue;
            }

            if (item.Price < 0)
            {
                expenseErrors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrValidation, $"Expense {item.Id}: Price must be non-negative."));
                continue;
            }

            var sourceExists = await _expenseRepository.SourceExistsAsync(item.SourceId, cancellationToken);
            if (!sourceExists)
            {
                expenseErrors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrReferenceNotFound, $"Expense {item.Id}: SourceId {item.SourceId} was not found."));
                continue;
            }

            var bankExists = await _expenseRepository.BankExistsAsync(item.BankId, cancellationToken);
            if (!bankExists)
            {
                expenseErrors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrReferenceNotFound, $"Expense {item.Id}: BankId {item.BankId} was not found."));
                continue;
            }

            acceptedExpenseIds.Add(item.Id);

            var existing = await _expenseRepository.GetByIdForUpdateIncludingDeletedAsync(item.Id, cancellationToken);
            if (existing is null)
            {
                expenseCreated += 1;

                if (!options.DryRun)
                {
                    var entity = TaxExpense.Create(item.Description, item.Date, item.BankId, item.Price, item.SourceId, _timeProvider);
                    entity.Id = item.Id;

                    if (item.IsDeleted == true)
                        entity.SoftDelete(_timeProvider);

                    await _expenseRepository.AddAsync(entity, cancellationToken);
                    trackedExpenses[item.Id] = entity;
                }

                continue;
            }

            if (options.Mode == DataTransferImportMode.InsertOnly)
            {
                expenseSkipped += 1;
                expenseWarnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnInsertOnlySkipped, $"Expense {item.Id}: skipped because it already exists in insertOnly mode."));
                trackedExpenses[item.Id] = existing;
                continue;
            }

            expenseUpdated += 1;

            if (!options.DryRun)
            {
                existing.UpdateDetails(item.Description, item.Date, item.BankId, item.Price, item.SourceId, _timeProvider);

                if (item.IsDeleted == true)
                    existing.SoftDelete(_timeProvider);
                else if (existing.IsDeleted)
                    existing.Restore(_timeProvider);
            }

            trackedExpenses[item.Id] = existing;
        }

        var tagWarnings = new List<DataTransferImportIssue>(modeWarnings);
        var tagErrors = new List<DataTransferImportIssue>();
        var tagCreated = 0;
        var tagSkipped = 0;

        var seenPairs = new HashSet<(Guid TaxExpenseId, Guid TagId)>();
        var requestedTagIds = expenseTags
            .Where(x => x.TagId != Guid.Empty)
            .Select(x => x.TagId)
            .Distinct()
            .ToList();

        var existingTagIds = await _expenseRepository.GetExistingTagIdsAsync(requestedTagIds, cancellationToken);
        var existingTagIdSet = existingTagIds.ToHashSet();

        foreach (var item in expenseTags)
        {
            if (item.TaxExpenseId == Guid.Empty || item.TagId == Guid.Empty)
            {
                tagErrors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrRequiredField, "ExpenseTag requires both TaxExpenseId and TagId."));
                continue;
            }

            var pair = (item.TaxExpenseId, item.TagId);
            if (!seenPairs.Add(pair))
            {
                tagSkipped += 1;
                tagWarnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnDuplicateSkipped, $"ExpenseTag ({item.TaxExpenseId}, {item.TagId}): duplicate pair in payload skipped."));
                continue;
            }

            if (!existingTagIdSet.Contains(item.TagId))
            {
                tagErrors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrReferenceNotFound, $"ExpenseTag ({item.TaxExpenseId}, {item.TagId}): TagId was not found."));
                continue;
            }

            trackedExpenses.TryGetValue(item.TaxExpenseId, out var expense);
            if (expense is null && !(options.DryRun && acceptedExpenseIds.Contains(item.TaxExpenseId)))
            {
                var expenseEntity = await _expenseRepository.GetByIdForUpdateIncludingDeletedAsync(item.TaxExpenseId, cancellationToken);
                if (expenseEntity is null)
                {
                    tagErrors.Add(new DataTransferImportIssue(DataTransferIssueCodes.ErrReferenceNotFound, $"ExpenseTag ({item.TaxExpenseId}, {item.TagId}): TaxExpenseId was not found."));
                    continue;
                }

                expense = expenseEntity;
                trackedExpenses[item.TaxExpenseId] = expenseEntity;
            }

            var alreadyLinked = expense?.TaxExpenseTags.Any(x => x.TagId == item.TagId) == true;
            if (alreadyLinked)
            {
                tagSkipped += 1;
                continue;
            }

            if (options.Mode == DataTransferImportMode.InsertOnly && expenses.Any(x => x.Id == item.TaxExpenseId) is false)
            {
                tagSkipped += 1;
                tagWarnings.Add(new DataTransferImportIssue(DataTransferIssueCodes.WarnInsertOnlySkipped, $"ExpenseTag ({item.TaxExpenseId}, {item.TagId}): skipped in insertOnly mode for existing expense."));
                continue;
            }

            tagCreated += 1;

            if (!options.DryRun)
            {
                var link = TaxExpenseTag.Create(item.TaxExpenseId, item.TagId);
                link.Id = item.Id == Guid.Empty ? link.Id : item.Id;
                expense!.TaxExpenseTags.Add(link);
            }
        }

        if (!options.DryRun && (expenseCreated > 0 || expenseUpdated > 0 || tagCreated > 0))
            await _expenseRepository.SaveChangesAsync(cancellationToken);

        return
        [
            new DataTransferEntityImportComputation("expenses", expenses.Count, expenseCreated, expenseUpdated, expenseSkipped, expenseWarnings, expenseErrors),
            new DataTransferEntityImportComputation("expenseTags", expenseTags.Count, tagCreated, 0, tagSkipped, tagWarnings, tagErrors),
        ];
    }
}
