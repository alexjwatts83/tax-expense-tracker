using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TaxExpenseTracker.Api.Options;

namespace TaxExpenseTracker.Tests.Integration;

public class EntraOptionsValidatorTests
{
    private readonly EntraOptionsValidator validator = new();

    [Theory]
    [InlineData("22222222-2222-2222-2222-222222222222")]
    [InlineData("api://22222222-2222-2222-2222-222222222222")]
    [InlineData("https://example.test/tax-expense-tracker")]
    public void ValidConfiguration_Succeeds(string audience)
    {
        var result = validator.Validate(null, CreateValidOptions(audience));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void InvalidConfiguration_ReportsEveryInvalidSetting()
    {
        var result = validator.Validate(null, new EntraOptions());

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Entra:TenantId"));
        Assert.Contains(result.Failures, failure => failure.Contains("Entra:Audience"));
        Assert.Contains(result.Failures, failure => failure.Contains("Entra:AllowedUserObjectId"));
    }

    [Theory]
    [InlineData("not-an-audience")]
    [InlineData("http://example.test/api")]
    [InlineData("api://")]
    public void InvalidAudience_Fails(string audience)
    {
        var result = validator.Validate(null, CreateValidOptions(audience));

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("Entra:Audience"));
    }

    [Fact]
    public void InvalidConfiguration_FailsApiStartup()
    {
        using var factory = new TaxExpenseTrackerApiFactory();
        using var invalidFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Entra:TenantId"] = "invalid",
                    ["Entra:Audience"] = "invalid",
                    ["Entra:AllowedUserObjectId"] = "invalid",
                })));

        var exception = Assert.Throws<OptionsValidationException>(() => invalidFactory.CreateClient());

        Assert.Contains("Entra:TenantId", exception.Message);
        Assert.Contains("Entra:Audience", exception.Message);
        Assert.Contains("Entra:AllowedUserObjectId", exception.Message);
    }

    private static EntraOptions CreateValidOptions(string audience) => new()
    {
        TenantId = "11111111-1111-1111-1111-111111111111",
        Audience = audience,
        AllowedUserObjectId = "33333333-3333-3333-3333-333333333333",
    };
}