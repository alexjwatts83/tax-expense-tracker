namespace TaxExpenseTracker.Application.Common;

public sealed class ApplicationValidationException(string message, string? paramName = null)
    : ArgumentException(message, paramName);

public sealed class ApplicationConflictException(string message)
    : InvalidOperationException(message);

public sealed class MissingReferenceException(string referenceType, Guid referenceId)
    : InvalidOperationException($"{referenceType} does not exist.")
{
    public string ReferenceType { get; } = referenceType;
    public Guid ReferenceId { get; } = referenceId;
}

public sealed class ResourceNotFoundException(string resourceType, Guid resourceId)
    : KeyNotFoundException($"{resourceType} was not found.")
{
    public string ResourceType { get; } = resourceType;
    public Guid ResourceId { get; } = resourceId;
}