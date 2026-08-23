using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaxExpenseTracker.Api.Authorization;
using TaxExpenseTracker.Api.Extensions;

namespace TaxExpenseTracker.Tests.Integration;

public class EntraAuthorizationPolicyTests
{
    [Theory]
    [InlineData(AuthorizationPolicies.AllowedUser)]
    [InlineData(AuthorizationPolicies.DataTransfer)]
    public async Task Disabled_AllowsAnonymousAccess(string policyName)
    {
        await using var provider = BuildProvider(enabled: false);
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        var result = await authorization.AuthorizeAsync(new ClaimsPrincipal(), null, policyName);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(AuthorizationPolicies.AllowedUser)]
    [InlineData(AuthorizationPolicies.DataTransfer)]
    public async Task Enabled_AllowsConfiguredAuthenticatedUser(string policyName)
    {
        await using var provider = BuildProvider(enabled: true);
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var user = CreateUser("33333333-3333-3333-3333-333333333333");

        var result = await authorization.AuthorizeAsync(user, null, policyName);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("44444444-4444-4444-4444-444444444444")]
    [InlineData("not-a-guid")]
    public async Task Enabled_DeniesMissingOrDifferentObjectId(string? objectId)
    {
        await using var provider = BuildProvider(enabled: true);
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var user = CreateUser(objectId);

        var allowedUserResult = await authorization.AuthorizeAsync(user, null, AuthorizationPolicies.AllowedUser);
        var dataTransferResult = await authorization.AuthorizeAsync(user, null, AuthorizationPolicies.DataTransfer);

        Assert.False(allowedUserResult.Succeeded);
        Assert.False(dataTransferResult.Succeeded);
    }

    [Fact]
    public async Task Enabled_DeniesUnauthenticatedMatchingObjectId()
    {
        await using var provider = BuildProvider(enabled: true);
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var identity = new ClaimsIdentity(
            [new Claim(AuthorizationPolicies.ObjectIdClaimType, "33333333-3333-3333-3333-333333333333")]);

        var result = await authorization.AuthorizeAsync(
            new ClaimsPrincipal(identity),
            null,
            AuthorizationPolicies.AllowedUser);

        Assert.False(result.Succeeded);
    }

    private static ServiceProvider BuildProvider(bool enabled)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Entra:Enabled"] = enabled.ToString(),
                ["Entra:AllowedUserObjectId"] = "33333333-3333-3333-3333-333333333333",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationAuthorization(configuration);
        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal CreateUser(string? objectId)
    {
        var claims = objectId is null
            ? []
            : new[] { new Claim(AuthorizationPolicies.ObjectIdClaimType, objectId) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}