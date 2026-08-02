namespace TaxExpenseTracker.Application.Common;

public static class ThrowHelper
{
    public static void Conflict(string message)
        => throw new ApplicationConflictException(message);

    public static void MissingReference(string referenceType, Guid referenceId)
        => throw new MissingReferenceException(referenceType, referenceId);

    public static void Validation(string message, string? paramName = null)
        => throw new ApplicationValidationException(message, paramName);

    public static void NotFound(string resourceType, Guid resourceId)
        => throw new ResourceNotFoundException(resourceType, resourceId);

    public static void ArgumentNull(string paramName)
        => throw new ArgumentNullException(paramName);
}