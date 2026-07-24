namespace TaxExpenseTracker.Application.DataTransfer;

public sealed class DataTransferTransactionalImportProcessor : IDataTransferTransactionalImportProcessor
{
    private readonly DataTransferExpenseImportHandler _expenseHandler;
    private readonly DataTransferWorkLocationImportHandler _workLocationHandler;
    private readonly DataTransferLeaveImportHandler _leaveHandler;
    private readonly IDataTransferImportResultFactory _resultFactory;

    public DataTransferTransactionalImportProcessor(
        DataTransferExpenseImportHandler expenseHandler,
        DataTransferWorkLocationImportHandler workLocationHandler,
        DataTransferLeaveImportHandler leaveHandler,
        IDataTransferImportResultFactory resultFactory)
    {
        _expenseHandler = expenseHandler;
        _workLocationHandler = workLocationHandler;
        _leaveHandler = leaveHandler;
        _resultFactory = resultFactory;
    }

    public async Task<DataTransferImportResultDto> ImportExpensesAsync(
        ExpenseImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var computations = await _expenseHandler.ImportAsync(payload, options, cancellationToken);
        var results = computations.Select(_resultFactory.BuildEntityResult).ToList();
        return new DataTransferImportResultDto(options.DryRun, options.Mode, results);
    }

    public async Task<DataTransferImportResultDto> ImportWorkLocationsAsync(
        WorkLocationImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var computation = await _workLocationHandler.ImportAsync(payload, options, cancellationToken);
        return new DataTransferImportResultDto(
            options.DryRun,
            options.Mode,
            [_resultFactory.BuildEntityResult(computation)]);
    }

    public async Task<DataTransferImportResultDto> ImportLeaveAsync(
        LeaveImportPayloadDto payload,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default)
    {
        var computation = await _leaveHandler.ImportAsync(payload, options, cancellationToken);
        return new DataTransferImportResultDto(
            options.DryRun,
            options.Mode,
            [_resultFactory.BuildEntityResult(computation)]);
    }
}
