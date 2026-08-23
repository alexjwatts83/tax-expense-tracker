using Microsoft.Extensions.Options;

namespace TaxExpenseTracker.Api.Options;

public sealed class EntraOptionsValidator : IValidateOptions<EntraOptions>
{
    public ValidateOptionsResult Validate(string? name, EntraOptions options)
    {
        var failures = new List<string>();

        if (!Guid.TryParse(options.TenantId, out _))
            failures.Add($"{EntraOptions.SectionName}:TenantId must be a valid GUID.");

        if (!IsValidAudience(options.Audience))
            failures.Add($"{EntraOptions.SectionName}:Audience must be a valid GUID or an absolute api:// or https:// URI.");

        if (!Guid.TryParse(options.AllowedUserObjectId, out _))
            failures.Add($"{EntraOptions.SectionName}:AllowedUserObjectId must be a valid GUID.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsValidAudience(string audience)
    {
        if (Guid.TryParse(audience, out _))
            return true;

        return Uri.TryCreate(audience, UriKind.Absolute, out var uri)
            && (uri.Scheme == "api" || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrWhiteSpace(uri.Host);
    }
}