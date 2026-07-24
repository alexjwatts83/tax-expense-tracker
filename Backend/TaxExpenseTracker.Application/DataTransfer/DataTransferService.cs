namespace TaxExpenseTracker.Application.DataTransfer;

public sealed class DataTransferService : IDataTransferService
{
    private readonly DataTransferExportService _exportService;
    private readonly IDataTransferTransactionCoordinator _transactionCoordinator;
    private readonly IDataTransferReferenceImportProcessor _referenceImportProcessor;
    private readonly IDataTransferTransactionalImportProcessor _transactionalImportProcessor;

    public DataTransferService(
        DataTransferExportService exportService,
        IDataTransferTransactionCoordinator transactionCoordinator,
        IDataTransferReferenceImportProcessor referenceImportProcessor,
        IDataTransferTransactionalImportProcessor transactionalImportProcessor)
    {
        _exportService = exportService;
        _transactionCoordinator = transactionCoordinator;
        _referenceImportProcessor = referenceImportProcessor;
        _transactionalImportProcessor = transactionalImportProcessor;
    }

    public Task<ReferenceDataExportEnvelopeDto> ExportReferenceDataAsync(
        bool includeSoftDeleted,
        CancellationToken cancellationToken = default)
    {
        return _exportService.ExportReferenceDataAsync(includeSoftDeleted, cancellationToken);
    }

    public Task<ExpenseImportPayloadDto> ExportExpensesAsync(
        bool includeSoftDeleted,
        CancellationToken cancellationToken = default)
    {
        return _exportService.ExportExpensesAsync(includeSoftDeleted, cancellationToken);
    }

    public Task<WorkLocationImportPayloadDto> ExportWorkLocationsAsync(
        bool includeSoftDeleted,
        CancellationToken cancellationToken = default)
    {
        return _exportService.ExportWorkLocationsAsync(includeSoftDeleted, cancellationToken);
    }

    public Task<LeaveImportPayloadDto> ExportLeaveAsync(
        bool includeSoftDeleted,
        CancellationToken cancellationToken = default)
    {
        return _exportService.ExportLeaveAsync(includeSoftDeleted, cancellationToken);
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
            action: ct => _referenceImportProcessor.ImportAsync(payload.Data, options, ct),
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
            action: ct => _transactionalImportProcessor.ImportExpensesAsync(payload, options, ct),
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
            action: ct => _transactionalImportProcessor.ImportWorkLocationsAsync(payload, options, ct),
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
            action: ct => _transactionalImportProcessor.ImportLeaveAsync(payload, options, ct),
            cancellationToken);
    }
}
