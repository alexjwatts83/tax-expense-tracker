using System.IO;

namespace TaxExpenseTrackerDevLauncher.Models;

public sealed record ApiLogFileInfo(string FullPath, DateTime LastWriteTime)
{
    public string Name => Path.GetFileName(FullPath);
    public string DisplayName => $"{Name}  ({LastWriteTime:g})";
}