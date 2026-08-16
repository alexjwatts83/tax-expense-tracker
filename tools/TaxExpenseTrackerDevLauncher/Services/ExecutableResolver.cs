using System.IO;

namespace TaxExpenseTrackerDevLauncher.Services;

public static class ExecutableResolver
{
    public static string Resolve(string executable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executable);

        if (Path.IsPathFullyQualified(executable))
            return File.Exists(executable)
                ? executable
                : throw new FileNotFoundException($"Executable '{executable}' does not exist.", executable);

        var executableNames = GetExecutableNames(executable);
        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var executableName in executableNames)
            {
                var candidate = Path.Combine(directory.Trim('"'), executableName);
                if (File.Exists(candidate))
                    return candidate;
            }
        }

        throw new FileNotFoundException(
            $"Could not find executable '{executable}' on PATH. Verify the required development tool is installed.",
            executable);
    }

    private static IReadOnlyList<string> GetExecutableNames(string executable)
    {
        if (Path.HasExtension(executable))
            return [executable];

        var pathExtensions = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT;.COM")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return pathExtensions
            .Select(extension => executable + extension.ToLowerInvariant())
            .Prepend(executable)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}