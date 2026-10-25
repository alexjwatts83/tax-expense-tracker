using System.IO;
using System.Text.Json;
using TaxExpenseTrackerDevLauncher.Models;

namespace TaxExpenseTrackerDevLauncher.Services;

public sealed record LauncherSettingsLoadResult(LauncherSettings Settings, string Warning);

public sealed class LauncherSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public LauncherSettingsStore(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TaxExpenseTrackerDevLauncher", "settings.json");
    }

    public string FilePath { get; }

    public LauncherSettingsLoadResult Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                var defaults = new LauncherSettings();
                Save(defaults);
                return new(defaults, $"Settings file was missing. Defaults saved to {FilePath}.");
            }

            var settings = JsonSerializer.Deserialize<LauncherSettings>(File.ReadAllText(FilePath), JsonOptions)
                ?? throw new InvalidDataException("Settings must be a JSON object.");
            settings.Validate();
            return new(settings, string.Empty);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            return new(new LauncherSettings(), $"Using default launcher settings. Check {FilePath}: {exception.Message}");
        }
    }

    public void Save(LauncherSettings settings)
    {
        settings.Validate();
        var directory = Path.GetDirectoryName(Path.GetFullPath(FilePath))!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $"settings-{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporaryPath, FilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}