namespace TaxExpenseTracker.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string AllowedUser = "AllowedUser";

    public const string DataTransfer = "DataTransfer";

    public const string ObjectIdClaimType = "oid";
}