using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TaxExpenseTracker.Api.Authentication;
using TaxExpenseTracker.Api.Extensions;

namespace TaxExpenseTracker.Tests.Integration;

public class DevelopmentAuthenticationTests
{
    [Fact]
    public async Task DevelopmentWithExplicitToggle_UsesDevelopmentScheme()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(entraEnabled: true, developmentSchemeEnabled: true);
        services.AddEntraAuthentication(configuration);

        var enabled = services.AddDevelopmentAuthentication(
            configuration,
            new TestEnvironment(Environments.Development));
        await using var provider = services.BuildServiceProvider();
        var scheme = await provider
            .GetRequiredService<IAuthenticationSchemeProvider>()
            .GetDefaultAuthenticateSchemeAsync();

        Assert.True(enabled);
        Assert.Equal(DevelopmentAuthenticationHandler.SchemeName, scheme?.Name);
    }

    [Fact]
    public async Task ProductionWithToggle_KeepsJwtBearerScheme()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(entraEnabled: true, developmentSchemeEnabled: true);
        services.AddEntraAuthentication(configuration);

        var enabled = services.AddDevelopmentAuthentication(
            configuration,
            new TestEnvironment(Environments.Production));
        await using var provider = services.BuildServiceProvider();
        var scheme = await provider
            .GetRequiredService<IAuthenticationSchemeProvider>()
            .GetDefaultAuthenticateSchemeAsync();

        Assert.False(enabled);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, scheme?.Name);
    }

    [Fact]
    public void DevelopmentScheme_RequiresEnforcedOwnerPolicies()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(entraEnabled: false, developmentSchemeEnabled: true);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddDevelopmentAuthentication(
                configuration,
                new TestEnvironment(Environments.Development)));

        Assert.Contains("Entra:Enabled=true", exception.Message);
    }

    private static IConfiguration CreateConfiguration(bool entraEnabled, bool developmentSchemeEnabled) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Entra:Enabled"] = entraEnabled.ToString(),
                ["Entra:TenantId"] = "11111111-1111-1111-1111-111111111111",
                ["Entra:Audience"] = "api://22222222-2222-2222-2222-222222222222",
                ["Entra:AllowedUserObjectId"] = "33333333-3333-3333-3333-333333333333",
                [AuthenticationExtensions.DevelopmentSchemeConfigKey] = developmentSchemeEnabled.ToString(),
            })
            .Build();

    private sealed class TestEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "TaxExpenseTracker.Tests.Integration";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = environmentName;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}