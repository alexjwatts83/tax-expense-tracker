using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaxExpenseTracker.Infrastructure.Data;

namespace TaxExpenseTracker.Api.Middleware;

public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<AppDbContext>().Database;
            return await database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("SQLite database is reachable.")
                : HealthCheckResult.Unhealthy("SQLite database is not reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("SQLite database health check failed.", exception);
        }
    }
}