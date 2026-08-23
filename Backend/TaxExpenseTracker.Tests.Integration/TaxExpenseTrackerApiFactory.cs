using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaxExpenseTracker.Api.Extensions;
using TaxExpenseTracker.Infrastructure.Data;

namespace TaxExpenseTracker.Tests.Integration;

public class TaxExpenseTrackerApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly bool authenticationEnabled;
    private readonly bool dataTransferEnabled;
    private readonly bool developmentAuthenticationEnabled;
    private readonly string environmentName;

    public TaxExpenseTrackerApiFactory(
        bool authenticationEnabled = false,
        bool dataTransferEnabled = false,
        bool developmentAuthenticationEnabled = false,
        string environmentName = "Testing")
    {
        this.authenticationEnabled = authenticationEnabled;
        this.dataTransferEnabled = dataTransferEnabled;
        this.developmentAuthenticationEnabled = developmentAuthenticationEnabled;
        this.environmentName = environmentName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();

        builder.UseEnvironment(environmentName);
        builder.UseSetting("Entra:Enabled", authenticationEnabled.ToString());
        builder.UseSetting("Entra:TenantId", "11111111-1111-1111-1111-111111111111");
        builder.UseSetting("Entra:Audience", "api://22222222-2222-2222-2222-222222222222");
        builder.UseSetting("Entra:AllowedUserObjectId", "33333333-3333-3333-3333-333333333333");
        builder.UseSetting("Features:EnableDataTransferEndpoints", dataTransferEnabled.ToString());
        builder.UseSetting(
            AuthenticationExtensions.DevelopmentSchemeConfigKey,
            developmentAuthenticationEnabled.ToString());
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Entra:Enabled"] = authenticationEnabled.ToString(),
                ["Entra:TenantId"] = "11111111-1111-1111-1111-111111111111",
                ["Entra:Audience"] = "api://22222222-2222-2222-2222-222222222222",
                ["Entra:AllowedUserObjectId"] = "33333333-3333-3333-3333-333333333333",
                ["Features:EnableDataTransferEndpoints"] = dataTransferEnabled.ToString(),
                [AuthenticationExtensions.DevelopmentSchemeConfigKey] = developmentAuthenticationEnabled.ToString(),
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>((sp, options) =>
                options.UseSqlite(connection).AddInterceptors(sp.GetServices<IInterceptor>()));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            connection.Dispose();
    }
}