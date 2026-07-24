using TaxExpenseTracker.Application.Expenses;
using TaxExpenseTracker.Application.Banks;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.PublicHolidays;
using TaxExpenseTracker.Application.Tags;
using TaxExpenseTracker.Application.Trackers;
using TaxExpenseTracker.Application.WorkLocation;
using TaxExpenseTracker.Application.Leave;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.DataTransfer;

public sealed class DataTransferService : IDataTransferService
{
    private const int SchemaVersion = 1;
    private const string AppName = "TaxExpenseTracker";

    private readonly IExpenseRepository _expenseRepository;
    private readonly IWorkLocationRepository _workLocationRepository;
    private readonly ILeaveRepository _leaveRepository;
    private readonly IDataTransferTransactionCoordinator _transactionCoordinator;
    private readonly ITrackerRepository _trackerRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IBankRepository _bankRepository;
    private readonly IPublicHolidayRepository _publicHolidayRepository;
    private readonly TimeProvider _timeProvider;

    public DataTransferService(
        IExpenseRepository expenseRepository,
        IWorkLocationRepository workLocationRepository,
        ILeaveRepository leaveRepository,
        IDataTransferTransactionCoordinator transactionCoordinator,
        ITrackerRepository trackerRepository,
        ITagRepository tagRepository,
        IBankRepository bankRepository,
        IPublicHolidayRepository publicHolidayRepository,
        TimeProvider timeProvider)
    {
        _expenseRepository = expenseRepository;
        _workLocationRepository = workLocationRepository;
        _leaveRepository = leaveRepository;
        _transactionCoordinator = transactionCoordinator;
        _trackerRepository = trackerRepository;
        _tagRepository = tagRepository;
        _bankRepository = bankRepository;
        _publicHolidayRepository = publicHolidayRepository;
        _timeProvider = timeProvider;
    }

    public async Task<ReferenceDataExportEnvelopeDto> ExportReferenceDataAsync(
        bool includeSoftDeleted,
        CancellationToken cancellationToken = default)
    {
        var trackers = includeSoftDeleted
            ? await _trackerRepository.GetAllIncludingDeletedAsync(cancellationToken)
            : await _trackerRepository.GetAllAsync(cancellationToken);

        var tags = includeSoftDeleted
            ? await _tagRepository.GetAllIncludingDeletedAsync(cancellationToken)
            : await _tagRepository.GetAllAsync(cancellationToken);

        var banks = includeSoftDeleted
            ? await _bankRepository.GetAllIncludingDeletedAsync(cancellationToken)
            : await _bankRepository.GetAllAsync(cancellationToken);

        var publicHolidays = await _publicHolidayRepository.GetAllAsync(cancellationToken);

        return new ReferenceDataExportEnvelopeDto(
            SchemaVersion,
            _timeProvider.GetUtcNow().UtcDateTime,
            new DataTransferSourceDto(AppName, "unknown"),
            new ReferenceDataExportDataDto(
                trackers.OrderBy(x => x.Name).Select(x => new ReferenceTrackerDto(x.Id, x.Name, x.Description, x.CreatedAt)).ToList(),
                tags.OrderBy(x => x.Name).Select(x => new ReferenceTagDto(x.Id, x.Name, x.Color, x.CreatedAt)).ToList(),
                banks.OrderBy(x => x.Name).Select(x => new ReferenceBankDto(x.Id, x.Name, x.CreatedAt)).ToList(),
                publicHolidays.Select(x => new ReferencePublicHolidayDto(
                    x.Id,
                    x.HolidayDate,
                    x.Name,
                    x.Source,
                    x.IsImported,
                    x.CanBeWorkedOn,
                    x.CreatedAt)).ToList()));
    }

    public async Task<ExpenseImportPayloadDto> ExportExpensesAsync(bool includeSoftDeleted, CancellationToken cancellationToken = default)
    {
        var expenses = includeSoftDeleted
            ? await _expenseRepository.GetAllIncludingDeletedAsync(cancellationToken)
            : await _expenseRepository.GetAllAsync(cancellationToken);

        var expenseDetails = new List<TaxExpense>();
        foreach (var expense in expenses.OrderByDescending(x => x.Date))
        {
            var full = await _expenseRepository.GetByIdForUpdateIncludingDeletedAsync(expense.Id, cancellationToken);
            if (full is not null)
            {
                expenseDetails.Add(full);
            }
        }

        var expenseItems = expenseDetails
            .Select(x => new ExpenseImportItemDto(
                x.Id,
                x.Date,
                x.Description,
                x.Price,
                x.BankId,
                x.SourceId,
                x.CreatedAt,
                x.UpdatedAt,
                x.IsDeleted))
            .ToList();

        var expenseTags = expenseDetails
            .SelectMany(x => x.TaxExpenseTags)
            .Select(x => new ExpenseTagImportItemDto(x.Id, x.TaxExpenseId, x.TagId))
            .ToList();

        return new ExpenseImportPayloadDto(expenseItems, expenseTags);
    }

    public async Task<WorkLocationImportPayloadDto> ExportWorkLocationsAsync(bool includeSoftDeleted, CancellationToken cancellationToken = default)
    {
        var entries = includeSoftDeleted
            ? await _workLocationRepository.GetAllIncludingDeletedAsync(cancellationToken)
            : await _workLocationRepository.GetAllAsync(cancellationToken);

        return new WorkLocationImportPayloadDto(
            entries
                .OrderByDescending(x => x.WorkDate)
                .Select(x => new WorkLocationEntryImportItemDto(
                    x.Id,
                    x.WorkDate,
                    x.EntryType,
                    x.EntryType == DayEntryType.SpecificHours ? x.HoursWorked : null,
                    x.Notes,
                    x.WorkLocation,
                    x.CreatedAt,
                    x.UpdatedAt,
                    x.IsDeleted))
                .ToList());
    }

    public async Task<LeaveImportPayloadDto> ExportLeaveAsync(bool includeSoftDeleted, CancellationToken cancellationToken = default)
    {
        var entries = includeSoftDeleted
            ? await _leaveRepository.GetAllIncludingDeletedAsync(cancellationToken)
            : await _leaveRepository.GetAllAsync(cancellationToken);

        return new LeaveImportPayloadDto(
            entries
                .OrderByDescending(x => x.LeaveDate)
                .Select(x => new LeaveEntryImportItemDto(
                    x.Id,
                    x.LeaveDate,
                    x.EntryType,
                    x.EntryType == DayEntryType.SpecificHours ? x.HoursWorked : null,
                    x.Notes,
                    x.LeaveType,
                    x.CreatedAt,
                    x.UpdatedAt,
                    x.IsDeleted))
                .ToList());
    }

    public Task<DataTransferImportResultDto> ImportReferenceDataAsync(
        ReferenceDataImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(payload.Data);

        return _transactionCoordinator.ExecuteAsync(
            useTransaction: !options.DryRun,
            action: ct => ImportReferenceDataCoreAsync(payload.Data, options, ct),
            cancellationToken);
    }

    public Task<DataTransferImportResultDto> ImportExpensesAsync(
        ExpenseImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return _transactionCoordinator.ExecuteAsync(
            useTransaction: !options.DryRun,
            action: ct => ImportExpensesCoreAsync(payload, options, ct),
            cancellationToken);
    }

    public Task<DataTransferImportResultDto> ImportWorkLocationsAsync(
        WorkLocationImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return _transactionCoordinator.ExecuteAsync(
            useTransaction: !options.DryRun,
            action: ct => ImportWorkLocationsCoreAsync(payload, options, ct),
            cancellationToken);
    }

    public Task<DataTransferImportResultDto> ImportLeaveAsync(
        LeaveImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return _transactionCoordinator.ExecuteAsync(
            useTransaction: !options.DryRun,
            action: ct => ImportLeaveCoreAsync(payload, options, ct),
            cancellationToken);
    }

    private static DataTransferEntityImportResultDto BuildPendingResult(string entity, int receivedCount)
    {
        return BuildEntityResult(
            entity,
            receivedCount,
            0,
            0,
            receivedCount,
            ["Import execution is pending implementation. This starter returns validation counts only."],
            []);
    }

    private async Task<DataTransferImportResultDto> ImportReferenceDataCoreAsync(
        ReferenceDataImportDataDto data,
        DataTransferImportOptions options,
        CancellationToken cancellationToken)
    {
        var results = new List<DataTransferEntityImportResultDto>();

        var modeWarnings = new List<string>();
        if (options.Mode == DataTransferImportMode.Replace && options.AllowDeletes)
        {
            modeWarnings.Add("Replace mode delete synchronization is not implemented yet; processing behaves like upsert.");
        }

        results.Add(await ImportTrackersAsync(data.Trackers ?? [], options, modeWarnings, cancellationToken));
        results.Add(await ImportTagsAsync(data.Tags ?? [], options, modeWarnings, cancellationToken));
        results.Add(await ImportBanksAsync(data.Banks ?? [], options, modeWarnings, cancellationToken));
        results.Add(await ImportPublicHolidaysAsync(data.PublicHolidays ?? [], options, modeWarnings, cancellationToken));

        return new DataTransferImportResultDto(options.DryRun, options.Mode, results);
    }

    private async Task<DataTransferEntityImportResultDto> ImportTrackersAsync(
        IReadOnlyList<ReferenceTrackerImportItemDto> items,
        DataTransferImportOptions options,
        IReadOnlyList<string> modeWarnings,
        CancellationToken cancellationToken)
    {
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var warnings = new List<string>(modeWarnings);
        var errors = new List<string>();

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                errors.Add($"Tracker {item.Id}: Name is required.");
                continue;
            }

            var existing = await _trackerRepository.GetByIdIncludingDeletedAsync(item.Id, cancellationToken);
            if (existing is null)
            {
                if (options.Mode == DataTransferImportMode.InsertOnly || options.Mode == DataTransferImportMode.Upsert || options.Mode == DataTransferImportMode.Replace)
                {
                    created += 1;

                    if (!options.DryRun)
                    {
                        var entity = Tracker.Create(item.Name, item.Description, _timeProvider);
                        entity.Id = item.Id;
                        await _trackerRepository.AddAsync(entity, cancellationToken);
                    }
                }

                continue;
            }

            if (options.Mode == DataTransferImportMode.InsertOnly)
            {
                skipped += 1;
                warnings.Add($"Tracker {item.Id}: skipped because it already exists in insertOnly mode.");
                continue;
            }

            updated += 1;

            if (!options.DryRun)
            {
                existing.Rename(item.Name, item.Description, _timeProvider);
                if (existing.IsDeleted)
                {
                    existing.Restore(_timeProvider);
                }
            }
        }

        if (!options.DryRun && (created > 0 || updated > 0))
        {
            await _trackerRepository.SaveChangesAsync(cancellationToken);
        }

        return BuildEntityResult("trackers", items.Count, created, updated, skipped, warnings, errors);
    }

    private async Task<DataTransferEntityImportResultDto> ImportTagsAsync(
        IReadOnlyList<ReferenceTagImportItemDto> items,
        DataTransferImportOptions options,
        IReadOnlyList<string> modeWarnings,
        CancellationToken cancellationToken)
    {
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var warnings = new List<string>(modeWarnings);
        var errors = new List<string>();

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                errors.Add($"Tag {item.Id}: Name is required.");
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
                warnings.Add($"Tag {item.Id}: skipped because it already exists in insertOnly mode.");
                continue;
            }

            updated += 1;

            if (!options.DryRun)
            {
                existing.Rename(item.Name);
                existing.SetColor(item.Color);
                if (existing.IsDeleted)
                {
                    existing.Restore(_timeProvider);
                }
            }
        }

        if (!options.DryRun && (created > 0 || updated > 0))
        {
            await _tagRepository.SaveChangesAsync(cancellationToken);
        }

        return BuildEntityResult("tags", items.Count, created, updated, skipped, warnings, errors);
    }

    private async Task<DataTransferEntityImportResultDto> ImportBanksAsync(
        IReadOnlyList<ReferenceBankImportItemDto> items,
        DataTransferImportOptions options,
        IReadOnlyList<string> modeWarnings,
        CancellationToken cancellationToken)
    {
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var warnings = new List<string>(modeWarnings);
        var errors = new List<string>();

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                errors.Add($"Bank {item.Id}: Name is required.");
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
                warnings.Add($"Bank {item.Id}: skipped because it already exists in insertOnly mode.");
                continue;
            }

            updated += 1;

            if (!options.DryRun)
            {
                existing.Rename(item.Name);
                if (existing.IsDeleted)
                {
                    existing.Restore(_timeProvider);
                }
            }
        }

        if (!options.DryRun && (created > 0 || updated > 0))
        {
            await _bankRepository.SaveChangesAsync(cancellationToken);
        }

        return BuildEntityResult("banks", items.Count, created, updated, skipped, warnings, errors);
    }

    private async Task<DataTransferEntityImportResultDto> ImportPublicHolidaysAsync(
        IReadOnlyList<ReferencePublicHolidayImportItemDto> items,
        DataTransferImportOptions options,
        IReadOnlyList<string> modeWarnings,
        CancellationToken cancellationToken)
    {
        var created = 0;
        var updated = 0;
        var skipped = 0;
        var warnings = new List<string>(modeWarnings);
        var errors = new List<string>();

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                errors.Add($"PublicHoliday {item.Id}: Name is required.");
                continue;
            }

            if (item.HolidayDate == default)
            {
                errors.Add($"PublicHoliday {item.Id}: HolidayDate is required.");
                continue;
            }

            var existing = await _publicHolidayRepository.GetByIdAsync(item.Id, cancellationToken);
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
                warnings.Add($"PublicHoliday {item.Id}: skipped because it already exists in insertOnly mode.");
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

        if (!options.DryRun && (created > 0 || updated > 0))
        {
            await _publicHolidayRepository.SaveChangesAsync(cancellationToken);
        }

        return BuildEntityResult("publicHolidays", items.Count, created, updated, skipped, warnings, errors);
    }

    private async Task<DataTransferImportResultDto> ImportExpensesCoreAsync(
        ExpenseImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken)
    {
        var expenses = payload.Expenses ?? [];
        var expenseTags = payload.ExpenseTags ?? [];

        var modeWarnings = new List<string>();
        if (options.Mode == DataTransferImportMode.Replace && options.AllowDeletes)
        {
            modeWarnings.Add("Replace mode delete synchronization is not implemented yet; processing behaves like upsert.");
        }

        var expenseWarnings = new List<string>(modeWarnings);
        var expenseErrors = new List<string>();
        var expenseCreated = 0;
        var expenseUpdated = 0;
        var expenseSkipped = 0;

        var trackedExpenses = new Dictionary<Guid, TaxExpense>();

        foreach (var item in expenses)
        {
            if (item.Id == Guid.Empty)
            {
                expenseErrors.Add("Expense Id is required.");
                continue;
            }

            if (item.BankId == Guid.Empty)
            {
                expenseErrors.Add($"Expense {item.Id}: BankId is required.");
                continue;
            }

            if (item.SourceId == Guid.Empty)
            {
                expenseErrors.Add($"Expense {item.Id}: SourceId is required.");
                continue;
            }

            if (item.Date == default)
            {
                expenseErrors.Add($"Expense {item.Id}: Date is required.");
                continue;
            }

            if (item.Price < 0)
            {
                expenseErrors.Add($"Expense {item.Id}: Price must be non-negative.");
                continue;
            }

            var sourceExists = await _expenseRepository.SourceExistsAsync(item.SourceId, cancellationToken);
            if (!sourceExists)
            {
                expenseErrors.Add($"Expense {item.Id}: SourceId {item.SourceId} was not found.");
                continue;
            }

            var bankExists = await _expenseRepository.BankExistsAsync(item.BankId, cancellationToken);
            if (!bankExists)
            {
                expenseErrors.Add($"Expense {item.Id}: BankId {item.BankId} was not found.");
                continue;
            }

            var existing = await _expenseRepository.GetByIdForUpdateIncludingDeletedAsync(item.Id, cancellationToken);
            if (existing is null)
            {
                expenseCreated += 1;

                if (!options.DryRun)
                {
                    var entity = TaxExpense.Create(item.Description, item.Date, item.BankId, item.Price, item.SourceId, _timeProvider);
                    entity.Id = item.Id;

                    if (item.IsDeleted == true)
                    {
                        entity.SoftDelete(_timeProvider);
                    }

                    await _expenseRepository.AddAsync(entity, cancellationToken);
                    trackedExpenses[item.Id] = entity;
                }

                continue;
            }

            if (options.Mode == DataTransferImportMode.InsertOnly)
            {
                expenseSkipped += 1;
                expenseWarnings.Add($"Expense {item.Id}: skipped because it already exists in insertOnly mode.");
                trackedExpenses[item.Id] = existing;
                continue;
            }

            expenseUpdated += 1;

            if (!options.DryRun)
            {
                existing.UpdateDetails(item.Description, item.Date, item.BankId, item.Price, item.SourceId, _timeProvider);

                if (item.IsDeleted == true)
                {
                    existing.SoftDelete(_timeProvider);
                }
                else if (existing.IsDeleted)
                {
                    existing.Restore(_timeProvider);
                }
            }

            trackedExpenses[item.Id] = existing;
        }

        var tagWarnings = new List<string>(modeWarnings);
        var tagErrors = new List<string>();
        var tagCreated = 0;
        var tagUpdated = 0;
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
                tagErrors.Add("ExpenseTag requires both TaxExpenseId and TagId.");
                continue;
            }

            var pair = (item.TaxExpenseId, item.TagId);
            if (!seenPairs.Add(pair))
            {
                tagSkipped += 1;
                tagWarnings.Add($"ExpenseTag ({item.TaxExpenseId}, {item.TagId}): duplicate pair in payload skipped.");
                continue;
            }

            if (!existingTagIdSet.Contains(item.TagId))
            {
                tagErrors.Add($"ExpenseTag ({item.TaxExpenseId}, {item.TagId}): TagId was not found.");
                continue;
            }

            if (!trackedExpenses.TryGetValue(item.TaxExpenseId, out var expense))
            {
                var expenseEntity = await _expenseRepository.GetByIdForUpdateIncludingDeletedAsync(item.TaxExpenseId, cancellationToken);
                if (expenseEntity is null)
                {
                    tagErrors.Add($"ExpenseTag ({item.TaxExpenseId}, {item.TagId}): TaxExpenseId was not found.");
                    continue;
                }

                expense = expenseEntity;
                trackedExpenses[item.TaxExpenseId] = expenseEntity;
            }

            var alreadyLinked = expense.TaxExpenseTags.Any(x => x.TagId == item.TagId);
            if (alreadyLinked)
            {
                tagSkipped += 1;
                continue;
            }

            if (options.Mode == DataTransferImportMode.InsertOnly && expenses.Any(x => x.Id == item.TaxExpenseId) is false)
            {
                tagSkipped += 1;
                tagWarnings.Add($"ExpenseTag ({item.TaxExpenseId}, {item.TagId}): skipped in insertOnly mode for existing expense.");
                continue;
            }

            tagCreated += 1;

            if (!options.DryRun)
            {
                var link = TaxExpenseTag.Create(item.TaxExpenseId, item.TagId);
                link.Id = item.Id == Guid.Empty ? link.Id : item.Id;
                expense.TaxExpenseTags.Add(link);
            }
        }

        if (!options.DryRun && (expenseCreated > 0 || expenseUpdated > 0 || tagCreated > 0))
        {
            await _expenseRepository.SaveChangesAsync(cancellationToken);
        }

        var results = new List<DataTransferEntityImportResultDto>
        {
            BuildEntityResult("expenses", expenses.Count, expenseCreated, expenseUpdated, expenseSkipped, expenseWarnings, expenseErrors),
            BuildEntityResult("expenseTags", expenseTags.Count, tagCreated, tagUpdated, tagSkipped, tagWarnings, tagErrors),
        };

        return new DataTransferImportResultDto(options.DryRun, options.Mode, results);
    }

    private async Task<DataTransferImportResultDto> ImportWorkLocationsCoreAsync(
        WorkLocationImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken)
    {
        var items = payload.WorkLocationEntries ?? [];
        var warnings = new List<string>();
        var errors = new List<string>();
        var created = 0;
        var updated = 0;
        var skipped = 0;

        if (options.Mode == DataTransferImportMode.Replace && options.AllowDeletes)
        {
            warnings.Add("Replace mode delete synchronization is not implemented yet; processing behaves like upsert.");
        }

        foreach (var item in items)
        {
            if (item.Id == Guid.Empty)
            {
                errors.Add("WorkLocationEntry Id is required.");
                continue;
            }

            if (item.WorkDate == default)
            {
                errors.Add($"WorkLocationEntry {item.Id}: WorkDate is required.");
                continue;
            }

            var existing = await _workLocationRepository.GetByIdIncludingDeletedAsync(item.Id, cancellationToken);
            var duplicateDateExists = await _workLocationRepository.ExistsForDateAsync(item.WorkDate, item.Id, cancellationToken);
            if (duplicateDateExists)
            {
                errors.Add($"WorkLocationEntry {item.Id}: another entry already exists for {item.WorkDate:yyyy-MM-dd}.");
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
                        {
                            entity.SoftDelete(_timeProvider);
                        }

                        await _workLocationRepository.AddAsync(entity, cancellationToken);
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException || ex is InvalidOperationException)
                    {
                        created -= 1;
                        errors.Add($"WorkLocationEntry {item.Id}: {ex.Message}");
                    }
                }

                continue;
            }

            if (options.Mode == DataTransferImportMode.InsertOnly)
            {
                skipped += 1;
                warnings.Add($"WorkLocationEntry {item.Id}: skipped because it already exists in insertOnly mode.");
                continue;
            }

            updated += 1;

            if (!options.DryRun)
            {
                try
                {
                    existing.Update(item.WorkDate, item.EntryType, item.SpecificHours, item.Notes, _timeProvider, item.WorkLocation);

                    if (item.IsDeleted == true)
                    {
                        existing.SoftDelete(_timeProvider);
                    }
                    else if (existing.IsDeleted)
                    {
                        existing.Restore(_timeProvider);
                    }
                }
                catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException || ex is InvalidOperationException)
                {
                    updated -= 1;
                    errors.Add($"WorkLocationEntry {item.Id}: {ex.Message}");
                }
            }
        }

        if (!options.DryRun && (created > 0 || updated > 0))
        {
            await _workLocationRepository.SaveChangesAsync(cancellationToken);
        }

        return new DataTransferImportResultDto(
            options.DryRun,
            options.Mode,
            [BuildEntityResult("workLocationEntries", items.Count, created, updated, skipped, warnings, errors)]);
    }

    private async Task<DataTransferImportResultDto> ImportLeaveCoreAsync(
        LeaveImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken)
    {
        var items = payload.LeaveEntries ?? [];
        var warnings = new List<string>();
        var errors = new List<string>();
        var created = 0;
        var updated = 0;
        var skipped = 0;

        if (options.Mode == DataTransferImportMode.Replace && options.AllowDeletes)
        {
            warnings.Add("Replace mode delete synchronization is not implemented yet; processing behaves like upsert.");
        }

        foreach (var item in items)
        {
            if (item.Id == Guid.Empty)
            {
                errors.Add("LeaveEntry Id is required.");
                continue;
            }

            if (item.LeaveDate == default)
            {
                errors.Add($"LeaveEntry {item.Id}: LeaveDate is required.");
                continue;
            }

            var existing = await _leaveRepository.GetByIdIncludingDeletedAsync(item.Id, cancellationToken);
            var duplicateDateExists = await _leaveRepository.ExistsForDateAsync(item.LeaveDate, item.Id, cancellationToken);
            if (duplicateDateExists)
            {
                errors.Add($"LeaveEntry {item.Id}: another entry already exists for {item.LeaveDate:yyyy-MM-dd}.");
                continue;
            }

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
                        {
                            entity.SoftDelete(_timeProvider);
                        }

                        await _leaveRepository.AddAsync(entity, cancellationToken);
                    }
                    catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException || ex is InvalidOperationException)
                    {
                        created -= 1;
                        errors.Add($"LeaveEntry {item.Id}: {ex.Message}");
                    }
                }

                continue;
            }

            if (options.Mode == DataTransferImportMode.InsertOnly)
            {
                skipped += 1;
                warnings.Add($"LeaveEntry {item.Id}: skipped because it already exists in insertOnly mode.");
                continue;
            }

            updated += 1;

            if (!options.DryRun)
            {
                try
                {
                    existing.Update(item.LeaveDate, item.EntryType, item.SpecificHours, item.Notes, _timeProvider, item.LeaveType);

                    if (item.IsDeleted == true)
                    {
                        existing.SoftDelete(_timeProvider);
                    }
                    else if (existing.IsDeleted)
                    {
                        existing.Restore(_timeProvider);
                    }
                }
                catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException || ex is InvalidOperationException)
                {
                    updated -= 1;
                    errors.Add($"LeaveEntry {item.Id}: {ex.Message}");
                }
            }
        }

        if (!options.DryRun && (created > 0 || updated > 0))
        {
            await _leaveRepository.SaveChangesAsync(cancellationToken);
        }

        return new DataTransferImportResultDto(
            options.DryRun,
            options.Mode,
            [BuildEntityResult("leaveEntries", items.Count, created, updated, skipped, warnings, errors)]);
    }

    private static DataTransferEntityImportResultDto BuildEntityResult(
        string entity,
        int receivedCount,
        int createdCount,
        int updatedCount,
        int skippedCount,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> errors)
    {
        return new DataTransferEntityImportResultDto(
            entity,
            receivedCount,
            createdCount,
            updatedCount,
            skippedCount,
            warnings,
            warnings.Select(MapWarningCode).ToList(),
            errors,
            errors.Select(MapErrorCode).ToList());
    }

    private static string MapWarningCode(string warning)
    {
        if (warning.Contains("insertOnly", StringComparison.OrdinalIgnoreCase))
        {
            return "WARN_INSERT_ONLY_SKIPPED";
        }

        if (warning.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
        {
            return "WARN_DUPLICATE_SKIPPED";
        }

        if (warning.Contains("replace mode", StringComparison.OrdinalIgnoreCase))
        {
            return "WARN_REPLACE_FALLBACK_TO_UPSERT";
        }

        return "WARN_GENERIC";
    }

    private static string MapErrorCode(string error)
    {
        if (error.Contains("required", StringComparison.OrdinalIgnoreCase))
        {
            return "ERR_REQUIRED_FIELD";
        }

        if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return "ERR_REFERENCE_NOT_FOUND";
        }

        if (error.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return "ERR_DUPLICATE_CONFLICT";
        }

        if (error.Contains("non-negative", StringComparison.OrdinalIgnoreCase)
            || error.Contains("invalid", StringComparison.OrdinalIgnoreCase)
            || error.Contains("must be", StringComparison.OrdinalIgnoreCase))
        {
            return "ERR_VALIDATION";
        }

        return "ERR_GENERIC";
    }
}
