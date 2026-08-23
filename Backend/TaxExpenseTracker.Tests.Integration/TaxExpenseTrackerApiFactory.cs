using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaxExpenseTracker.Infrastructure.Data;

namespace TaxExpenseTracker.Tests.Integration;

public class TaxExpenseTrackerApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly bool authenticationEnabled;
    private readonly bool dataTransferEnabled;

    public TaxExpenseTrackerApiFactory(
        bool authenticationEnabled = false,
        bool dataTransferEnabled = false)
    {
        this.authenticationEnabled = authenticationEnabled;
        this.dataTransferEnabled = dataTransferEnabled;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();

        builder.UseEnvironment("Testing");
        builder.UseSetting("Entra:Enabled", authenticationEnabled.ToString());
        builder.UseSetting("Entra:TenantId", "11111111-1111-1111-1111-111111111111");
        builder.UseSetting("Entra:Audience", "api://22222222-2222-2222-2222-222222222222");
        builder.UseSetting("Entra:AllowedUserObjectId", "33333333-3333-3333-3333-333333333333");
        builder.UseSetting("Features:EnableDataTransferEndpoints", dataTransferEnabled.ToString());
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Entra:Enabled"] = authenticationEnabled.ToString(),
                ["Entra:TenantId"] = "11111111-1111-1111-1111-111111111111",
                ["Entra:Audience"] = "api://22222222-2222-2222-2222-222222222222",
                ["Entra:AllowedUserObjectId"] = "33333333-3333-3333-3333-333333333333",
                ["Features:EnableDataTransferEndpoints"] = dataTransferEnabled.ToString(),
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            connection.Dispose();
    }
}