namespace TaxExpenseTracker.Application.DataTransfer;

public sealed class DataTransferImportResultFactory : IDataTransferImportResultFactory
{
    public DataTransferEntityImportResultDto BuildEntityResult(DataTransferEntityImportComputation computation)
    {
        return new DataTransferEntityImportResultDto(
            computation.Entity,
            computation.ReceivedCount,
            computation.CreatedCount,
            computation.UpdatedCount,
            computation.SkippedCount,
            computation.Warnings.Select(x => x.Message).ToList(),
            computation.Warnings.Select(x => x.Code).ToList(),
            computation.Errors.Select(x => x.Message).ToList(),
            computation.Errors.Select(x => x.Code).ToList());
    }
}
