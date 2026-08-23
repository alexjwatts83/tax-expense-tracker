using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace TaxExpenseTracker.Tests.Integration;

public class DataTransferExportBoundaryTests
{
    [Theory]
    [InlineData("trackers", "Trackers", new[] { "Tags", "Banks", "PublicHolidays" })]
    [InlineData("tags", "Tags", new[] { "Trackers", "Banks", "PublicHolidays" })]
    [InlineData("banks", "Banks", new[] { "Trackers", "Tags", "PublicHolidays" })]
    [InlineData("public-holidays", "PublicHolidays", new[] { "Trackers", "Tags", "Banks" })]
    public async Task PerEntityReferenceExport_QueriesOnlyOwningTable(
        string route,
        string ownedTable,
        string[] unrelatedTables)
    {
        var interceptor = new QueryRecordingInterceptor();
        using var factory = new TaxExpenseTrackerApiFactory(dataTransferEnabled: true)
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                services.AddSingleton<IInterceptor>(interceptor)));
        using var client = factory.CreateClient();

        // Startup migrations and seeding query every table; measure only the export request.
        interceptor.Commands.Clear();

        using var response = await client.GetAsync($"/api/data-transfer/export/{route}");
        response.EnsureSuccessStatusCode();

        Assert.Contains(interceptor.Commands, x => x.Contains($"\"{ownedTable}\"", StringComparison.Ordinal));
        Assert.All(unrelatedTables, table =>
            Assert.DoesNotContain(interceptor.Commands, x => x.Contains($"\"{table}\"", StringComparison.Ordinal)));
    }

    private sealed class QueryRecordingInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
