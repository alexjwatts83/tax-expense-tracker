using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxExpenseTracker.Api.Extensions;
using TaxExpenseTracker.Application.Banks;
using TaxExpenseTracker.Application.DataTransfer;
using TaxExpenseTracker.Application.Expenses;
using TaxExpenseTracker.Application.Leave;
using TaxExpenseTracker.Application.PublicHolidays;
using TaxExpenseTracker.Application.Tags;
using TaxExpenseTracker.Application.Trackers;
using TaxExpenseTracker.Application.WorkLocation;
using TaxExpenseTracker.Infrastructure.Data;

namespace TaxExpenseTracker.Tests.Integration;

public class DataTransferRoundtripTests
{
    [Fact]
    public async Task LargeReferenceExport_ImportedIntoCleanDatabase_RestoresExpenseRelationships()
    {
        const int EntityCount = 500;
        await using var source = await TestDatabase.CreateAsync();
        await using var destination = await TestDatabase.CreateAsync();
        var sourceService = source.Services.GetRequiredService<IDataTransferService>();
        var destinationService = destination.Services.GetRequiredService<IDataTransferService>();
        var trackerId = Guid.NewGuid();
        var bankId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var sourcePayload = new ReferenceDataImportPayloadDto(new ReferenceDataImportDataDto(
            Enumerable.Range(0, EntityCount)
                .Select(index => new ReferenceTrackerImportItemDto(index == 0 ? trackerId : Guid.NewGuid(), $"Tracker {index}", null))
                .ToList(),
            Enumerable.Range(0, EntityCount)
                .Select(index => new ReferenceTagImportItemDto(index == 0 ? tagId : Guid.NewGuid(), $"Tag {index}", "#336699"))
                .ToList(),
            Enumerable.Range(0, EntityCount)
                .Select(index => new ReferenceBankImportItemDto(index == 0 ? bankId : Guid.NewGuid(), $"Bank {index}"))
                .ToList(),
            []));

        var sourceImport = await sourceService.ImportReferenceDataAsync(sourcePayload, new DataTransferImportOptions());
        Assert.All(sourceImport.Results, result => Assert.Empty(result.Errors));

        var exported = await sourceService.ExportReferenceDataAsync(includeSoftDeleted: true);
        var restorePayload = new ReferenceDataImportPayloadDto(new ReferenceDataImportDataDto(
            exported.Data.Trackers.Select(x => new ReferenceTrackerImportItemDto(x.Id, x.Name, x.Description)).ToList(),
            exported.Data.Tags.Select(x => new ReferenceTagImportItemDto(x.Id, x.Name, x.Color)).ToList(),
            exported.Data.Banks.Select(x => new ReferenceBankImportItemDto(x.Id, x.Name)).ToList(),
            exported.Data.PublicHolidays.Select(x => new ReferencePublicHolidayImportItemDto(x.Id, x.HolidayDate, x.Name, x.Source, x.CanBeWorkedOn)).ToList()));

        var restoreResult = await destinationService.ImportReferenceDataAsync(restorePayload, new DataTransferImportOptions());
        Assert.All(restoreResult.Results, result => Assert.Empty(result.Errors));

        var expenseId = Guid.NewGuid();
        var expenseResult = await destinationService.ImportExpensesAsync(
            new ExpenseImportPayloadDto(
                [new ExpenseImportItemDto(expenseId, new DateTime(2026, 8, 1), "Roundtrip expense", 42.50m, bankId, trackerId, null, null, false)],
                [new ExpenseTagImportItemDto(Guid.NewGuid(), expenseId, tagId)]),
            new DataTransferImportOptions());

        Assert.All(expenseResult.Results, result => Assert.Empty(result.Errors));
        var exportedExpenses = await destinationService.ExportExpensesAsync(includeSoftDeleted: true);
        var exportedExpenseItems = Assert.IsAssignableFrom<IReadOnlyList<ExpenseImportItemDto>>(exportedExpenses.Expenses);
        var exportedExpenseTags = Assert.IsAssignableFrom<IReadOnlyList<ExpenseTagImportItemDto>>(exportedExpenses.ExpenseTags);
        var expense = Assert.Single(exportedExpenseItems, x => x.Id == expenseId);
        var expenseTag = Assert.Single(exportedExpenseTags, x => x.TaxExpenseId == expense.Id);
        Assert.Equal(tagId, expenseTag.TagId);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private TestDatabase(SqliteConnection connection, ServiceProvider services)
        {
            _connection = connection;
            Services = services;
        }

        public ServiceProvider Services { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<ITrackerRepository, EfTrackerRepository>();
            services.AddScoped<ITagRepository, EfTagRepository>();
            services.AddScoped<IBankRepository, EfBankRepository>();
            services.AddScoped<IExpenseRepository, EfExpenseRepository>();
            services.AddScoped<IWorkLocationRepository, EfWorkLocationRepository>();
            services.AddScoped<ILeaveRepository, EfLeaveRepository>();
            services.AddScoped<IPublicHolidayRepository, EfPublicHolidayRepository>();
            services.AddDataTransferServices();
            var serviceProvider = services.BuildServiceProvider();
            await serviceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
            return new TestDatabase(connection, serviceProvider);
        }

        public async ValueTask DisposeAsync()
        {
            await Services.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}