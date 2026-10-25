using TaxExpenseTrackerDevLauncher.Models;
using TaxExpenseTrackerDevLauncher.Services;

namespace TaxExpenseTrackerDevLauncher.Tests;

public sealed class LauncherSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"LauncherSettings-{Guid.NewGuid():N}");

    private LauncherSettingsStore CreateStore() => new(Path.Combine(_directory, "settings.json"));

    [Fact]
    public void MissingFile_PersistsCurrentDefaultsWithNotice()
    {
        var store = CreateStore();
        var result = store.Load();

        Assert.Equal(new LauncherSettings(), result.Settings);
        Assert.Equal(60, result.Settings.StartupTimeoutSeconds);
        Assert.Equal(2, result.Settings.GracefulStopTimeoutSeconds);
        Assert.Equal(5000, result.Settings.MaximumLogLines);
        Assert.Equal(@"C:\logs\TaxExpenseTracker.Api", result.Settings.ApiLogFolder);
        Assert.NotEmpty(result.Warning);
        Assert.True(File.Exists(store.FilePath));
        Assert.Empty(store.Load().Warning);
    }

    [Fact]
    public void Save_RoundTripsValidatedSettings()
    {
        var store = CreateStore();
        var settings = new LauncherSettings
        {
            StartupTimeoutSeconds = 120,
            GracefulStopTimeoutSeconds = 10,
            MaximumLogLines = 10000,
            ApiLogFolder = _directory
        };

        store.Save(settings);

        Assert.Equal(settings, store.Load().Settings);
        Assert.Empty(store.Load().Warning);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void PartialFile_UsesDefaultsForMissingProperties()
    {
        var store = CreateStore();
        Directory.CreateDirectory(_directory);
        File.WriteAllText(store.FilePath, "{\"startupTimeoutSeconds\":120}");

        var result = store.Load();

        Assert.Equal(new LauncherSettings { StartupTimeoutSeconds = 120 }, result.Settings);
        Assert.Empty(result.Warning);
    }

    [Theory]
    [InlineData("not JSON")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"startupTimeoutSeconds\":0}")]
    [InlineData("{\"startupTimeoutSeconds\":601}")]
    [InlineData("{\"gracefulStopTimeoutSeconds\":0}")]
    [InlineData("{\"gracefulStopTimeoutSeconds\":61}")]
    [InlineData("{\"maximumLogLines\":99}")]
    [InlineData("{\"maximumLogLines\":100001}")]
    [InlineData("{\"apiLogFolder\":null}")]
    [InlineData("{\"apiLogFolder\":\"relative/path\"}")]
    [InlineData("{\"startupTimeoutSeconds\":\"slow\"}")]
    public void InvalidFile_FallsBackWithWarningAndPreservesOriginal(string content)
    {
        var store = CreateStore();
        Directory.CreateDirectory(_directory);
        File.WriteAllText(store.FilePath, content);

        var result = store.Load();

        Assert.Equal(new LauncherSettings(), result.Settings);
        Assert.Contains(store.FilePath, result.Warning);
        Assert.Equal(content, File.ReadAllText(store.FilePath));
    }

    [Theory]
    [InlineData(1, 1, 100)]
    [InlineData(600, 60, 100000)]
    public void Bounds_AreAccepted(int startup, int stop, int lines)
    {
        var store = CreateStore();
        var settings = new LauncherSettings
        {
            StartupTimeoutSeconds = startup,
            GracefulStopTimeoutSeconds = stop,
            MaximumLogLines = lines
        };
        store.Save(settings);

        Assert.Equal(settings, store.Load().Settings);
    }

    [Fact]
    public void InvalidSave_DoesNotReplaceExistingFile()
    {
        var store = CreateStore();
        store.Save(new LauncherSettings());

        Assert.Throws<InvalidDataException>(() => store.Save(new LauncherSettings { MaximumLogLines = -1 }));
        Assert.Equal(new LauncherSettings(), store.Load().Settings);
    }

    [Fact]
    public void UnwritableLocation_FallsBackWithWarning()
    {
        Directory.CreateDirectory(_directory);
        var parentFile = Path.Combine(_directory, "not-a-directory");
        File.WriteAllText(parentFile, string.Empty);
        var store = new LauncherSettingsStore(Path.Combine(parentFile, "settings.json"));

        var result = store.Load();

        Assert.Equal(new LauncherSettings(), result.Settings);
        Assert.NotEmpty(result.Warning);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}