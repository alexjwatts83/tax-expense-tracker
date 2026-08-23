using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TaxExpenseTracker.Api.Options;

namespace TaxExpenseTracker.Api.Extensions;

public static class AuthenticationExtensions
{
    public static bool AddEntraAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var entra = configuration
            .GetSection(EntraOptions.SectionName)
            .Get<EntraOptions>()
            ?? new EntraOptions();

        if (!entra.Enabled)
            return false;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"https://login.microsoftonline.com/{entra.TenantId}/v2.0";
                options.Audience = entra.Audience;
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = true;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                };
            });

        return true;
    }
}