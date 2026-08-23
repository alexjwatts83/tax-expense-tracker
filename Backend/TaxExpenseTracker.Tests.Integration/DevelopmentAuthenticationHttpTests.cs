using System.Net;
using Microsoft.Extensions.Hosting;
using TaxExpenseTracker.Api.Authentication;

namespace TaxExpenseTracker.Tests.Integration;

public class DevelopmentAuthenticationHttpTests
{
    private const string AllowedUserObjectId = "33333333-3333-3333-3333-333333333333";

    [Fact]
    public async Task AnonymousRequest_ReturnsUnauthorized()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/banks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DifferentUser_ReturnsForbidden()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            DevelopmentAuthenticationHandler.ObjectIdHeaderName,
            "44444444-4444-4444-4444-444444444444");

        using var response = await client.GetAsync("/api/banks");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/banks")]
    [InlineData("/api/data-transfer/export/trackers")]
    public async Task ConfiguredOwner_CanAccessProtectedEndpoints(string path)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            DevelopmentAuthenticationHandler.ObjectIdHeaderName,
            AllowedUserObjectId);

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static TaxExpenseTrackerApiFactory CreateFactory() => new(
        authenticationEnabled: true,
        dataTransferEnabled: true,
        developmentAuthenticationEnabled: true,
        environmentName: Environments.Development);
}