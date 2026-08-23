using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace TaxExpenseTracker.Tests.Integration;

public class ApiRouteCoverageTests
{
    [Fact]
    public void EveryControllerAction_HasOneUniqueAttributedHttpRoute()
    {
        using var factory = new TaxExpenseTrackerApiFactory();
        var actions = factory.Services
            .GetRequiredService<IActionDescriptorCollectionProvider>()
            .ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .ToList();

        Assert.NotEmpty(actions);
        Assert.All(actions, action =>
        {
            Assert.False(string.IsNullOrWhiteSpace(action.AttributeRouteInfo?.Template));
            Assert.Single(action.ActionConstraints!.OfType<HttpMethodActionConstraint>());
        });

        var routes = actions.SelectMany(action =>
            action.ActionConstraints!
                .OfType<HttpMethodActionConstraint>()
                .Single()
                .HttpMethods
                .Select(method => $"{method} {action.AttributeRouteInfo!.Template}"));

        Assert.DoesNotContain(routes.GroupBy(route => route), group => group.Count() > 1);
    }
}