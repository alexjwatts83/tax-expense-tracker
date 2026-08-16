using System.IO;
using TaxExpenseTrackerDevLauncher.Models;

namespace TaxExpenseTrackerDevLauncher.Services;

public static class ServiceRegistry
{
    public static IReadOnlyList<ServiceDefinition> Create(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        return
        [
            new ServiceDefinition(
                "api",
                "API",
                "dotnet",
                ["run", "--project", "Backend/TaxExpenseTracker.Api", "--launch-profile", "https"],
                repositoryRoot,
                [7152, 5158],
                "Now listening on:",
                new Uri("https://localhost:7152/swagger")),
            new ServiceDefinition(
                "web",
                "Web",
                "npm.cmd",
                ["start"],
                Path.Combine(repositoryRoot, "Frontend"),
                [4200],
                "Local:",
                new Uri("http://localhost:4200"))
        ];
    }
}