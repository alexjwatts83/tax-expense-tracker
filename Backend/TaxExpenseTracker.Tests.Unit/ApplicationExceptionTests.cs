using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Tests.Unit;

public class ApplicationExceptionTests
{
    [Fact]
    public void ThrowHelper_ThrowsTypedValidationFailure()
    {
        var exception = Assert.Throws<ApplicationValidationException>(() =>
            ThrowHelper.Validation("Name is required.", "name"));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void ThrowHelper_ThrowsTypedConflictFailure()
    {
        Assert.Throws<ApplicationConflictException>(() =>
            ThrowHelper.Conflict("An active entry already exists."));
    }

    [Fact]
    public void ThrowHelper_ThrowsTypedMissingReferenceFailure()
    {
        var referenceId = Guid.NewGuid();

        var exception = Assert.Throws<MissingReferenceException>(() =>
            ThrowHelper.MissingReference("Tag", referenceId));

        Assert.Equal("Tag", exception.ReferenceType);
        Assert.Equal(referenceId, exception.ReferenceId);
    }

    [Fact]
    public void ThrowHelper_ThrowsTypedNotFoundFailure()
    {
        var resourceId = Guid.NewGuid();

        var exception = Assert.Throws<ResourceNotFoundException>(() =>
            ThrowHelper.NotFound("Expense", resourceId));

        Assert.Equal("Expense", exception.ResourceType);
        Assert.Equal(resourceId, exception.ResourceId);
    }
}