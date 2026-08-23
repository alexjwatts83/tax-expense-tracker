using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TaxExpenseTracker.Api.Authentication;
using TaxExpenseTracker.Api.Options;

namespace TaxExpenseTracker.Api.Extensions;

public static class AuthenticationExtensions
{
    public const string DevelopmentSchemeConfigKey = "Authentication:UseDevelopmentTestScheme";

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

    public static bool AddDevelopmentAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>(DevelopmentSchemeConfigKey))
            return false;

        if (!configuration.GetValue<bool>("Entra:Enabled"))
        {
            throw new InvalidOperationException(
                $"{DevelopmentSchemeConfigKey} requires Entra:Enabled=true so owner policies remain enforced.");
        }

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = DevelopmentAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = DevelopmentAuthenticationHandler.SchemeName;
                options.DefaultForbidScheme = DevelopmentAuthenticationHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>(
                DevelopmentAuthenticationHandler.SchemeName,
                _ => { });

        return true;
    }
}