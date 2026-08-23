using System.Security.Claims;
using TaxExpenseTracker.Api.Authorization;
using TaxExpenseTracker.Api.Options;

namespace TaxExpenseTracker.Api.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddApplicationAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var entra = configuration
            .GetSection(EntraOptions.SectionName)
            .Get<EntraOptions>()
            ?? new EntraOptions();
        Guid.TryParse(entra.AllowedUserObjectId, out var allowedUserObjectId);

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.AllowedUser,
                policy => ConfigureOwnerPolicy(policy, entra.Enabled, allowedUserObjectId));
            options.AddPolicy(
                AuthorizationPolicies.DataTransfer,
                policy => ConfigureOwnerPolicy(policy, entra.Enabled, allowedUserObjectId));
        });

        return services;
    }

    private static void ConfigureOwnerPolicy(
        Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder policy,
        bool authenticationEnabled,
        Guid allowedUserObjectId)
    {
        if (!authenticationEnabled)
        {
            policy.RequireAssertion(_ => true);
            return;
        }

        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var objectId = context.User.FindFirstValue(AuthorizationPolicies.ObjectIdClaimType);
            return Guid.TryParse(objectId, out var parsedObjectId)
                && parsedObjectId == allowedUserObjectId;
        });
    }
}