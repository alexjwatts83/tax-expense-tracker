namespace TaxExpenseTracker.Application.DataTransfer;

public sealed class DataTransferReferenceImportProcessor : IDataTransferReferenceImportProcessor
{
    private readonly DataTransferTrackerImportHandler _trackerHandler;
    private readonly DataTransferTagImportHandler _tagHandler;
    private readonly DataTransferBankImportHandler _bankHandler;
    private readonly DataTransferPublicHolidayImportHandler _publicHolidayHandler;
    private readonly IDataTransferImportResultFactory _resultFactory;

    public DataTransferReferenceImportProcessor(
        DataTransferTrackerImportHandler trackerHandler,
        DataTransferTagImportHandler tagHandler,
        DataTransferBankImportHandler bankHandler,
        DataTransferPublicHolidayImportHandler publicHolidayHandler,
        IDataTransferImportResultFactory resultFactory)
    {
        _trackerHandler = trackerHandler;
        _tagHandler = tagHandler;
        _bankHandler = bankHandler;
        _publicHolidayHandler = publicHolidayHandler;
        _resultFactory = resultFactory;
    }

    public async Task<DataTransferImportResultDto> ImportAsync(
        ReferenceDataImportDataDto data,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var computations = new[]
        {
            await _trackerHandler.ImportAsync(data.Trackers ?? [], options, cancellationToken),
            await _tagHandler.ImportAsync(data.Tags ?? [], options, cancellationToken),
            await _bankHandler.ImportAsync(data.Banks ?? [], options, cancellationToken),
            await _publicHolidayHandler.ImportAsync(data.PublicHolidays ?? [], options, cancellationToken),
        };

        var results = computations
            .Select(_resultFactory.BuildEntityResult)
            .ToList();

        return new DataTransferImportResultDto(options.DryRun, options.Mode, results);
    }
}
