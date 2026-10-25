using System.IO;

namespace TaxExpenseTrackerDevLauncher.Models;

public sealed record LauncherSettings
{
    public int StartupTimeoutSeconds { get; init; } = 60;
    public int GracefulStopTimeoutSeconds { get; init; } = 2;
    public string ApiLogFolder { get; init; } = @"C:\logs\TaxExpenseTracker.Api";
    public int MaximumLogLines { get; init; } = 5000;
    public bool AutoRestartEnabled { get; init; }
    public int MaximumRestartAttempts { get; init; } = 3;
    public int RestartBackoffSeconds { get; init; } = 2;

    public void Validate()
    {
        if (StartupTimeoutSeconds is < 1 or > 600)
            throw new InvalidDataException("StartupTimeoutSeconds must be between 1 and 600.");
        if (GracefulStopTimeoutSeconds is < 1 or > 60)
            throw new InvalidDataException("GracefulStopTimeoutSeconds must be between 1 and 60.");
        if (MaximumLogLines is < 100 or > 100000)
            throw new InvalidDataException("MaximumLogLines must be between 100 and 100000.");
        if (MaximumRestartAttempts is < 1 or > 10)
            throw new InvalidDataException("MaximumRestartAttempts must be between 1 and 10.");
        if (RestartBackoffSeconds is < 1 or > 60)
            throw new InvalidDataException("RestartBackoffSeconds must be between 1 and 60.");
        if (string.IsNullOrWhiteSpace(ApiLogFolder) || !Path.IsPathFullyQualified(ApiLogFolder) ||
            ApiLogFolder.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            throw new InvalidDataException("ApiLogFolder must be an absolute directory path.");
    }
}