using TaxExpenseTracker.Application.Banks;
using TaxExpenseTracker.Application.Expenses;
using TaxExpenseTracker.Application.Leave;
using TaxExpenseTracker.Application.PublicHolidays;
using TaxExpenseTracker.Application.Tags;
using TaxExpenseTracker.Application.Trackers;
using TaxExpenseTracker.Application.WorkLocation;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Application.DataTransfer;

public sealed class DataTransferExportService
{
    private const int SchemaVersion = 1;
    private const string AppName = "TaxExpenseTracker";

    private readonly IExpenseRepository _expenseRepository;
    private readonly IWorkLocationRepository _workLocationRepository;
    private readonly ILeaveRepository _leaveRepository;
    private readonly ITrackerRepository _trackerRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IBankRepository _bankRepository;
    private readonly IPublicHolidayRepository _publicHolidayRepository;
    private readonly TimeProvider _timeProvider;

    public DataTransferExportService(
        IExpenseRepository expenseRepository,
        IWorkLocationRepository workLocationRepository,
        ILeaveRepository leaveRepository,
        ITrackerRepository trackerRepository,
        ITagRepository tagRepository,
        IBankRepository bankRepository,
        IPublicHolidayRepository publicHolidayRepository,
        TimeProvider timeProvider)
    {
        _expenseRepository = expenseRepository;
        _workLocationRepository = workLocationRepository;
        _leaveRepository = leaveRepository;
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

    public async Task<ExpenseImportPayloadDto> ExportExpensesAsync(
        bool includeSoftDeleted,
        CancellationToken cancellationToken = default)
    {
        var expenseDetails = await _expenseRepository.GetAllForExportAsync(includeSoftDeleted, cancellationToken);

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

    public async Task<WorkLocationImportPayloadDto> ExportWorkLocationsAsync(
        bool includeSoftDeleted,
        CancellationToken cancellationToken = default)
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

    public async Task<LeaveImportPayloadDto> ExportLeaveAsync(
        bool includeSoftDeleted,
        CancellationToken cancellationToken = default)
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
}
