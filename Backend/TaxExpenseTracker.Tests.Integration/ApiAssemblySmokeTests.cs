using TaxExpenseTracker.Api.Controllers;

namespace TaxExpenseTracker.Tests.Integration;

public class ApiAssemblySmokeTests
{
    [Fact]
    public void Api_Controllers_AreDiscoverable()
    {
        Assert.NotNull(typeof(ExpensesController));
        Assert.NotNull(typeof(TrackersController));
        Assert.NotNull(typeof(TagsController));
    }
}
