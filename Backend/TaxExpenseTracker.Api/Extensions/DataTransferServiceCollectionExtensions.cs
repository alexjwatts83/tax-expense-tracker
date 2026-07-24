using TaxExpenseTracker.Application.DataTransfer;
using TaxExpenseTracker.Infrastructure.Data;

namespace TaxExpenseTracker.Api.Extensions;

public static class DataTransferServiceCollectionExtensions
{
    public static IServiceCollection AddDataTransferServices(this IServiceCollection services)
    {
        services.AddScoped<IDataTransferTransactionCoordinator, EfDataTransferTransactionCoordinator>();
        services.AddScoped<IDataTransferImportResultFactory, DataTransferImportResultFactory>();
        services.AddScoped<DataTransferExportService>();
        services.AddScoped<DataTransferTrackerImportHandler>();
        services.AddScoped<DataTransferTagImportHandler>();
        services.AddScoped<DataTransferBankImportHandler>();
        services.AddScoped<DataTransferPublicHolidayImportHandler>();
        services.AddScoped<DataTransferExpenseImportHandler>();
        services.AddScoped<DataTransferWorkLocationImportHandler>();
        services.AddScoped<DataTransferLeaveImportHandler>();
        services.AddScoped<IDataTransferReferenceImportProcessor, DataTransferReferenceImportProcessor>();
        services.AddScoped<IDataTransferTransactionalImportProcessor, DataTransferTransactionalImportProcessor>();
        services.AddScoped<IDataTransferService, DataTransferService>();

        return services;
    }
}
