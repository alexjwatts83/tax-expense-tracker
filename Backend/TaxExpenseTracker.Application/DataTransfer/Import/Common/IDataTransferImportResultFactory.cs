namespace TaxExpenseTracker.Application.DataTransfer;

public interface IDataTransferImportResultFactory
{
    DataTransferEntityImportResultDto BuildEntityResult(DataTransferEntityImportComputation computation);
}
