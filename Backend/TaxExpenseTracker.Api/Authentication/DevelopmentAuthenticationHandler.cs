using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using TaxExpenseTracker.Api.Authorization;

namespace TaxExpenseTracker.Api.Authentication;

public sealed class DevelopmentAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DevelopmentTest";
    public const string ObjectIdHeaderName = "X-Development-Object-Id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var objectId = Request.Headers[ObjectIdHeaderName].ToString().Trim();
        if (string.IsNullOrWhiteSpace(objectId))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(
            [new Claim(AuthorizationPolicies.ObjectIdClaimType, objectId)],
            SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}