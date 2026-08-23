using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaxExpenseTracker.Api.Extensions;

namespace TaxExpenseTracker.Tests.Integration;

public class EntraAuthenticationTests
{
    [Fact]
    public void Disabled_DoesNotRegisterAuthentication()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(enabled: false);

        var enabled = services.AddEntraAuthentication(configuration);

        Assert.False(enabled);
        Assert.DoesNotContain(services, service => service.ServiceType == typeof(IAuthenticationSchemeProvider));
    }

    [Fact]
    public async Task Enabled_RegistersStrictBearerValidation()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(enabled: true);

        var enabled = services.AddEntraAuthentication(configuration);
        await using var provider = services.BuildServiceProvider();
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var scheme = await schemeProvider.GetDefaultAuthenticateSchemeAsync();
        var options = provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.True(enabled);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, scheme?.Name);
        Assert.Equal(
            "https://login.microsoftonline.com/11111111-1111-1111-1111-111111111111/v2.0",
            options.Authority);
        Assert.Equal("api://22222222-2222-2222-2222-222222222222", options.Audience);
        Assert.False(options.MapInboundClaims);
        Assert.True(options.RequireHttpsMetadata);
        Assert.False(options.SaveToken);
        Assert.True(options.TokenValidationParameters.ValidateIssuer);
        Assert.True(options.TokenValidationParameters.ValidateAudience);
        Assert.True(options.TokenValidationParameters.ValidateIssuerSigningKey);
        Assert.True(options.TokenValidationParameters.ValidateLifetime);
        Assert.Equal(TimeSpan.FromMinutes(2), options.TokenValidationParameters.ClockSkew);
    }

    private static IConfiguration CreateConfiguration(bool enabled) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Entra:Enabled"] = enabled.ToString(),
                ["Entra:TenantId"] = "11111111-1111-1111-1111-111111111111",
                ["Entra:Audience"] = "api://22222222-2222-2222-2222-222222222222",
                ["Entra:AllowedUserObjectId"] = "33333333-3333-3333-3333-333333333333",
            })
            .Build();
}