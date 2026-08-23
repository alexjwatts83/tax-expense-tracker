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
using TaxExpenseTracker.Domain.Entities;
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
            exported.Data.Trackers.Select(x => new ReferenceTrackerImportItemDto(x.Id, x.Name, x.Description, x.CreatedAt, x.UpdatedAt)).ToList(),
            exported.Data.Tags.Select(x => new ReferenceTagImportItemDto(x.Id, x.Name, x.Color, x.CreatedAt, x.UpdatedAt)).ToList(),
            exported.Data.Banks.Select(x => new ReferenceBankImportItemDto(x.Id, x.Name, x.CreatedAt, x.UpdatedAt)).ToList(),
            exported.Data.PublicHolidays.Select(x => new ReferencePublicHolidayImportItemDto(x.Id, x.HolidayDate, x.Name, x.Source, x.CanBeWorkedOn, x.CreatedAt, x.UpdatedAt)).ToList()));

        var restoreResult = await destinationService.ImportReferenceDataAsync(restorePayload, new DataTransferImportOptions());
        Assert.All(restoreResult.Results, result => Assert.Empty(result.Errors));
        var restored = await destinationService.ExportReferenceDataAsync(includeSoftDeleted: true);
        Assert.Equal(exported.Data.Trackers.Single(x => x.Id == trackerId), restored.Data.Trackers.Single(x => x.Id == trackerId));
        Assert.Equal(exported.Data.Tags.Single(x => x.Id == tagId), restored.Data.Tags.Single(x => x.Id == tagId));
        Assert.Equal(exported.Data.Banks.Single(x => x.Id == bankId), restored.Data.Banks.Single(x => x.Id == bankId));
        var restoredHoliday = restored.Data.PublicHolidays.Single(x => x.Id == exported.Data.PublicHolidays[0].Id);
        Assert.Equal(exported.Data.PublicHolidays[0].CreatedAt, restoredHoliday.CreatedAt);
        Assert.Equal(exported.Data.PublicHolidays[0].UpdatedAt, restoredHoliday.UpdatedAt);

        var expenseId = Guid.NewGuid();
        var expenseResult = await destinationService.ImportExpensesAsync(
            new ExpenseImportPayloadDto(
                [new ExpenseImportItemDto(expenseId, new DateOnly(2026, 8, 1), "Roundtrip expense", 42.50m, bankId, trackerId, null, null, false)],
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

    [Fact]
    public async Task TimeEntries_ImportedIntoCleanDatabase_RoundtripEncapsulatedState()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = database.Services.GetRequiredService<IDataTransferService>();
        var workId = Guid.NewGuid();
        var leaveId = Guid.NewGuid();

        var workResult = await service.ImportWorkLocationsAsync(
            new WorkLocationImportPayloadDto([
                new WorkLocationEntryImportItemDto(
                    workId,
                    new DateOnly(2026, 8, 3),
                    DayEntryType.SpecificHours,
                    4.5m,
                    "Office morning",
                    WorkLocationType.Office,
                    null,
                    null,
                    false),
            ]),
            new DataTransferImportOptions());
        var leaveResult = await service.ImportLeaveAsync(
            new LeaveImportPayloadDto([
                new LeaveEntryImportItemDto(
                    leaveId,
                    new DateOnly(2026, 8, 4),
                    DayEntryType.HalfDay,
                    null,
                    "Medical appointment",
                    LeaveType.Sick,
                    null,
                    null,
                    false),
            ]),
            new DataTransferImportOptions());

        Assert.All(workResult.Results, result => Assert.Empty(result.Errors));
        Assert.All(leaveResult.Results, result => Assert.Empty(result.Errors));

        var exportedWork = Assert.Single((await service.ExportWorkLocationsAsync(true)).WorkLocationEntries!);
        var exportedLeave = Assert.Single((await service.ExportLeaveAsync(true)).LeaveEntries!);
        Assert.Equal(workId, exportedWork.Id);
        Assert.Equal(WorkLocationType.Office, exportedWork.WorkLocation);
        Assert.Equal(4.5m, exportedWork.SpecificHours);
        Assert.Equal("Office morning", exportedWork.Notes);
        Assert.Equal(leaveId, exportedLeave.Id);
        Assert.Equal(LeaveType.Sick, exportedLeave.LeaveType);
        Assert.Equal(DayEntryType.HalfDay, exportedLeave.EntryType);
        Assert.Equal("Medical appointment", exportedLeave.Notes);
    }

    [Fact]
    public async Task ReferenceImport_WithEntityError_RollsBackAllEntitiesInOneTransaction()
    {
        await using var database = await TestDatabase.CreateAsync();
        var trackerId = Guid.NewGuid();

        // A valid tracker shares the payload with a tag whose blank name fails validation.
        var invalidResult = await ImportReferenceAsync(database, new ReferenceDataImportPayloadDto(new ReferenceDataImportDataDto(
            [new ReferenceTrackerImportItemDto(trackerId, "Valid tracker", null)],
            [new ReferenceTagImportItemDto(Guid.NewGuid(), "  ", "#336699")],
            [],
            [])));

        Assert.Contains(invalidResult.Results, r => r.Entity == "tags" && r.Errors.Count > 0);
        Assert.DoesNotContain((await ExportReferenceAsync(database)).Data.Trackers, x => x.Id == trackerId);

        var validResult = await ImportReferenceAsync(database, new ReferenceDataImportPayloadDto(new ReferenceDataImportDataDto(
            [new ReferenceTrackerImportItemDto(trackerId, "Valid tracker", null)],
            [],
            [],
            [])));

        Assert.All(validResult.Results, r => Assert.Empty(r.Errors));
        Assert.Contains((await ExportReferenceAsync(database)).Data.Trackers, x => x.Id == trackerId);
    }

    private static async Task<DataTransferImportResultDto> ImportReferenceAsync(TestDatabase database, ReferenceDataImportPayloadDto payload)
    {
        using var scope = database.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDataTransferService>();
        return await service.ImportReferenceDataAsync(payload, new DataTransferImportOptions());
    }

    private static async Task<ReferenceDataExportEnvelopeDto> ExportReferenceAsync(TestDatabase database)
    {
        using var scope = database.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IDataTransferService>();
        return await service.ExportReferenceDataAsync(includeSoftDeleted: true);
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