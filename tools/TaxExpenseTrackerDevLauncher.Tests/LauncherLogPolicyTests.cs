using TaxExpenseTrackerDevLauncher.Models;

namespace TaxExpenseTrackerDevLauncher.Tests;

public sealed class LauncherLogPolicyTests
{
    [Fact]
    public void LogLineCollection_EvictsOldestLinesAtCapacity()
    {
        var lines = new LogLineCollection(3);

        for (var index = 1; index <= 5; index++)
            lines.Add(CreateLine("api", $"line {index}"));

        Assert.Equal(3, lines.Count);
        Assert.Equal(["line 3", "line 4", "line 5"], lines.Select(line => line.Text));
    }

    [Fact]
    public void LauncherLogFilter_AppliesServiceAndCaseInsensitiveTextFilters()
    {
        var apiError = CreateLine("api", "Database ERROR");
        var webError = CreateLine("web", "Build error");

        Assert.False(LauncherLogFilter.Include(apiError, showApiLogs: false, showWebLogs: true, "error"));
        Assert.True(LauncherLogFilter.Include(webError, showApiLogs: false, showWebLogs: true, "ERROR"));
        Assert.False(LauncherLogFilter.Include(webError, showApiLogs: true, showWebLogs: true, "warning"));
    }

    [Fact]
    public void LauncherLogFilter_FrontendViewIncludesOnlyMatchingWebLines()
    {
        Assert.True(LauncherLogFilter.IncludeFrontend(CreateLine("web", "Angular ready"), "READY"));
        Assert.False(LauncherLogFilter.IncludeFrontend(CreateLine("api", "API ready"), "ready"));
        Assert.False(LauncherLogFilter.IncludeFrontend(CreateLine("web", "Angular ready"), "failed"));
    }

    private static LogLine CreateLine(string serviceId, string text) =>
        new(DateTimeOffset.UtcNow, serviceId, "stdout", text);
}