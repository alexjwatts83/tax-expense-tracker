using NetArchTest.Rules;

namespace TaxExpenseTracker.Tests.Integration;

public class ArchitectureTests
{
    [Fact]
    public void Domain_DoesNotDependOnOuterLayers()
    {
        var result = Types
            .InAssembly(typeof(Domain.Entities.Entity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "TaxExpenseTracker.Application",
                "TaxExpenseTracker.Infrastructure",
                "TaxExpenseTracker.Api")
            .GetResult();

        AssertArchitecture(result);
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        var result = Types
            .InAssembly(typeof(Application.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("TaxExpenseTracker.Infrastructure", "TaxExpenseTracker.Api")
            .GetResult();

        AssertArchitecture(result);
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types
            .InAssembly(typeof(Infrastructure.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("TaxExpenseTracker.Api")
            .GetResult();

        AssertArchitecture(result);
    }

    private static void AssertArchitecture(TestResult result)
    {
        Assert.True(
            result.IsSuccessful,
            $"Forbidden dependencies found: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}