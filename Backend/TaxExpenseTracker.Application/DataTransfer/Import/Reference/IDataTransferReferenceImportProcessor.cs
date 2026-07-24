namespace TaxExpenseTracker.Application.DataTransfer;

public interface IDataTransferReferenceImportProcessor
{
    Task<DataTransferImportResultDto> ImportAsync(
        ReferenceDataImportDataDto data,
        DataTransferImportOptions options,
        CancellationToken cancellationToken = default);
}
