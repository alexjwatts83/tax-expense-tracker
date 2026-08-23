namespace TaxExpenseTracker.Api.Options;

public sealed class EntraOptions
{
    public const string SectionName = "Entra";

    public string TenantId { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string AllowedUserObjectId { get; set; } = string.Empty;
}