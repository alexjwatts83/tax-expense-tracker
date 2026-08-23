using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxExpenseTracker.Api.Authorization;
using TaxExpenseTracker.Api.Controllers;

namespace TaxExpenseTracker.Tests.Integration;

public class ControllerAuthorizationTests
{
    [Fact]
    public void EveryController_HasExpectedPolicy_AndNoAnonymousEndpoints()
    {
        var controllers = typeof(Program).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(controllers);
        Assert.All(controllers, controller =>
        {
            var expectedPolicy = controller == typeof(DataTransferController)
                ? AuthorizationPolicies.DataTransfer
                : AuthorizationPolicies.AllowedUser;
            var authorizeAttributes = controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
            var anonymousAttributes = controller.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
                .Concat(controller
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .SelectMany(method => method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)));

            Assert.Contains(authorizeAttributes, attribute => attribute.Policy == expectedPolicy);
            Assert.Empty(anonymousAttributes);
        });
    }
}