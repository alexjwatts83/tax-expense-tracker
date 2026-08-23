using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaxExpenseTracker.Api.Authorization;

namespace TaxExpenseTracker.Tests.Integration;

public class ApiAuthorizationTests
{
    private const string AllowedUserObjectId = "33333333-3333-3333-3333-333333333333";

    [Theory]
    [InlineData("/api/banks")]
    [InlineData("/api/data-transfer/export/trackers")]
    public async Task AnonymousRequest_ReturnsUnauthorized(string path)
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("44444444-4444-4444-4444-444444444444")]
    public async Task AuthenticatedNonOwner_ReturnsForbidden(string? objectId)
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();
        SetTestUser(client, objectId);

        using var response = await client.GetAsync("/api/banks");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/banks")]
    [InlineData("/api/data-transfer/export/trackers")]
    public async Task ConfiguredOwner_CanAccessProtectedEndpoint(string path)
    {
        using var factory = new AuthenticatedApiFactory();
        using var client = factory.CreateClient();
        SetTestUser(client, AllowedUserObjectId);

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static void SetTestUser(HttpClient client, string? objectId)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthenticationHandler.SchemeName,
            objectId ?? "missing");
    }

    private sealed class AuthenticatedApiFactory()
        : TaxExpenseTrackerApiFactory(authenticationEnabled: true, dataTransferEnabled: true)
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.SchemeName,
                        _ => { });
            });
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var header = Request.Headers.Authorization.ToString();
            if (!header.StartsWith($"{SchemeName} ", StringComparison.Ordinal))
                return Task.FromResult(AuthenticateResult.NoResult());

            var objectId = header[SchemeName.Length..].Trim();
            var claims = objectId == "missing"
                ? []
                : new[] { new Claim(AuthorizationPolicies.ObjectIdClaimType, objectId) };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}