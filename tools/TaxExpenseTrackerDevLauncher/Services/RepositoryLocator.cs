using System.IO;

namespace TaxExpenseTrackerDevLauncher.Services;

public static class RepositoryLocator
{
    private const string SolutionFileName = "TaxExpenseTracker.sln";

    public static string FindRepositoryRoot(string? startPath = null)
    {
        var current = new DirectoryInfo(startPath ?? AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, SolutionFileName)))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not find {SolutionFileName} above '{startPath ?? AppContext.BaseDirectory}'. " +
            "Run the launcher from inside the Tax Expense Tracker repository.");
    }
}