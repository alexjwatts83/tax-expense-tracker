using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaxExpenseTracker.Infrastructure.Data;

namespace TaxExpenseTracker.Tests.Integration;

public class OperationalReadinessTests
{
    [Fact]
    public async Task HealthEndpoint_ReportsDatabaseReady_WithCorrelationHeader()
    {
        using var factory = new TaxExpenseTrackerApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", "health-correlation-id");

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        Assert.Equal("health-correlation-id", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public void BlankConnectionString_FailsAtStartupWithActionableMessage()
    {
        using var factory = new BlankConnectionFactory();

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("ConnectionStrings__DefaultConnection", exception.Message);
    }

    [Fact]
    public async Task DisabledStartupMigrations_LeavesDatabaseUnmigrated()
    {
        using var factory = new MigrationsDisabledFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

        Assert.Empty(appliedMigrations);
    }

    private sealed class BlankConnectionFactory : TaxExpenseTrackerApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("ConnectionStrings:DefaultConnection", " ");
        }
    }

    private sealed class MigrationsDisabledFactory : TaxExpenseTrackerApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:ApplyMigrationsOnStartup"] = "false",
                }));
        }
    }
}