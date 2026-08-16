namespace TaxExpenseTrackerDevLauncher.Models;

public sealed record ServiceDefinition(
    string Id,
    string DisplayName,
    string Executable,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyList<int> Ports,
    string ReadyLogPattern,
    Uri LocalUri);