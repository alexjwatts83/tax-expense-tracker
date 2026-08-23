using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TaxExpenseTracker.Application.DataTransfer;
using TaxExpenseTracker.Application.Expenses;
using TaxExpenseTracker.Infrastructure.Data;
using Xunit.Abstractions;

namespace TaxExpenseTracker.Tests.Integration;

public class ExpenseDataTransferProfileTests(ITestOutputHelper output)
{
    private const string BankId = "F2C328B0-6D89-4B66-8EF4-FCBE9970A1FD";
    private const string TrackerId = "E13E64D9-BF27-4232-9AF4-B2DB537D5FAF";

    [Fact]
    public async Task ExpenseReadsAndExport_HaveBoundedQueryCountsAndReportMeasurements()
    {
        var expenseCount = GetExpenseCount();
        var interceptor = new QueryCountingInterceptor();
        using var factory = new TaxExpenseTrackerApiFactory(dataTransferEnabled: true)
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                services.AddSingleton<IInterceptor>(interceptor)));
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedExpensesAsync(context, expenseCount);
        var repository = scope.ServiceProvider.GetRequiredService<IExpenseRepository>();

        interceptor.Reset();
        var stopwatch = Stopwatch.StartNew();
        var page = await repository.GetPagedWithDetailsAsync(pageNumber: 10, pageSize: 50);
        stopwatch.Stop();
        output.WriteLine($"paging: rows={page.Items.Count}/{page.TotalCount}, queries={interceptor.Count}, elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F2}");
        Assert.Equal(expenseCount, page.TotalCount);
        Assert.Equal(50, page.Items.Count);
        Assert.Equal(2, interceptor.Count);

        interceptor.Reset();
        stopwatch.Restart();
        var filtered = await repository.FilterWithDetailsAsync(new ExpenseFilterQuery(
            Date: new DateOnly(2026, 1, 15),
            BankId: Guid.Parse(BankId),
            Price: null,
            SourceId: Guid.Parse(TrackerId),
            TagIds: []));
        stopwatch.Stop();
        output.WriteLine($"filtering: rows={filtered.Count}, queries={interceptor.Count}, elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F2}");
        Assert.Equal(1, interceptor.Count);

        interceptor.Reset();
        stopwatch.Restart();
        var export = await scope.ServiceProvider.GetRequiredService<IDataTransferService>()
            .ExportExpensesAsync(includeSoftDeleted: true);
        var serializedBytes = JsonSerializer.SerializeToUtf8Bytes(export, new JsonSerializerOptions(JsonSerializerDefaults.Web)).Length;
        stopwatch.Stop();
        output.WriteLine($"dataTransfer: rows={export.Expenses!.Count}, queries={interceptor.Count}, bytes={serializedBytes}, elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F2}");
        Assert.Equal(expenseCount, export.Expenses.Count);
        Assert.Equal(1, interceptor.Count);
    }

    private static int GetExpenseCount()
    {
        return int.TryParse(Environment.GetEnvironmentVariable("PERF_EXPENSE_COUNT"), out var count) && count >= 500
            ? count
            : 1_000;
    }

    private static Task SeedExpensesAsync(AppDbContext context, int count)
    {
        return context.Database.ExecuteSqlRawAsync($$"""
            WITH digits(n) AS (
                VALUES (0), (1), (2), (3), (4), (5), (6), (7), (8), (9)
            ), sequence(n) AS (
                SELECT ones.n + 10 * tens.n + 100 * hundreds.n + 1000 * thousands.n + 10000 * tenThousands.n + 1
                FROM digits ones
                CROSS JOIN digits tens
                CROSS JOIN digits hundreds
                CROSS JOIN digits thousands
                CROSS JOIN digits tenThousands
            )
            INSERT INTO "TaxExpenses" (
                "Id", "BankId", "CreatedAt", "Date", "Description", "IsDeleted", "Price", "SourceId", "UpdatedAt")
            SELECT
                printf('%08x-0000-4000-8000-%012x', n, n),
                '{{BankId}}',
                '2026-01-01 00:00:00',
                date('2026-01-01', printf('+%d days', n % 365)),
                printf('Profile expense %d', n),
                0,
                printf('%.2f', (n % 500) + 1),
                '{{TrackerId}}',
                '2026-01-01 00:00:00'
            FROM sequence
            WHERE n <= {{count}};
            """);
    }

    private sealed class QueryCountingInterceptor : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public void Reset() => Count = 0;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Count++;
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}