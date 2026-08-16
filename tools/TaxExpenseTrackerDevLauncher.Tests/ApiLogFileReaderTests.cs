using TaxExpenseTrackerDevLauncher.Models;
using TaxExpenseTrackerDevLauncher.Services;

namespace TaxExpenseTrackerDevLauncher.Tests;

public sealed class ApiLogFileReaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"ApiLogFileReader-{Guid.NewGuid():N}");

    public ApiLogFileReaderTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public async Task RefreshFilesAsync_ReturnsNewestFileFirst()
    {
        var olderPath = Path.Combine(_directory, "older.log");
        var newerPath = Path.Combine(_directory, "newer.log");
        await File.WriteAllTextAsync(olderPath, "older");
        await File.WriteAllTextAsync(newerPath, "newer");
        File.SetLastWriteTime(olderPath, DateTime.Now.AddDays(-1));
        File.SetLastWriteTime(newerPath, DateTime.Now);
        IReadOnlyList<ApiLogFileInfo>? discoveredFiles = null;
        using var reader = new ApiLogFileReader(_directory, watchForChanges: false);
        reader.FilesChanged += files => discoveredFiles = files;

        await reader.RefreshFilesAsync();

        Assert.NotNull(discoveredFiles);
        Assert.Equal([newerPath, olderPath], discoveredFiles.Select(file => file.FullPath));
    }

    [Fact]
    public async Task RefreshFilesAsync_ReportsEmptyDirectory()
    {
        IReadOnlyList<ApiLogFileInfo>? discoveredFiles = null;
        string? status = null;
        using var reader = new ApiLogFileReader(_directory, watchForChanges: false);
        reader.FilesChanged += files => discoveredFiles = files;
        reader.StatusChanged += value => status = value;

        await reader.RefreshFilesAsync();

        Assert.Empty(discoveredFiles ?? [new ApiLogFileInfo("unexpected", default)]);
        Assert.Equal("No API log files found.", status);
    }

    [Fact]
    public async Task RefreshFilesAsync_ReportsMissingDirectory()
    {
        var missingDirectory = Path.Combine(_directory, "missing");
        IReadOnlyList<ApiLogFileInfo>? discoveredFiles = null;
        string? status = null;
        using var reader = new ApiLogFileReader(missingDirectory, watchForChanges: false);
        reader.FilesChanged += files => discoveredFiles = files;
        reader.StatusChanged += value => status = value;

        await reader.RefreshFilesAsync();

        Assert.Empty(discoveredFiles ?? [new ApiLogFileInfo("unexpected", default)]);
        Assert.Contains("does not exist", status);
    }

    [Fact]
    public async Task ReadUpdatesAsync_AppendsWithoutLockingFile()
    {
        var path = Path.Combine(_directory, "api.log");
        await File.WriteAllTextAsync(path, "first line\n");
        IReadOnlyList<string>? initialLines = null;
        IReadOnlyList<string>? appendedLines = null;
        using var reader = new ApiLogFileReader(_directory, watchForChanges: false);
        reader.LinesReset += lines => initialLines = lines;
        reader.LinesAppended += lines => appendedLines = lines;

        await reader.SelectFileAsync(path);
        await File.AppendAllTextAsync(path, "second line\n");
        await reader.ReadUpdatesAsync();

        Assert.Equal(["first line"], initialLines);
        Assert.Equal(["second line"], appendedLines);
    }

    [Fact]
    public async Task ReadUpdatesAsync_PreservesLineSplitAcrossReads()
    {
        var path = Path.Combine(_directory, "api.log");
        await File.WriteAllTextAsync(path, "initial line\npartial");
        var appendedBatches = new List<IReadOnlyList<string>>();
        using var reader = new ApiLogFileReader(_directory, watchForChanges: false);
        reader.LinesAppended += lines => appendedBatches.Add(lines);

        await reader.SelectFileAsync(path);
        await File.AppendAllTextAsync(path, " line\n");
        await reader.ReadUpdatesAsync();

        var appended = Assert.Single(appendedBatches);
        Assert.Equal(["partial line"], appended);
    }

    [Fact]
    public async Task SelectFileAsync_LoadsOnlyLastFiveThousandLines()
    {
        var path = Path.Combine(_directory, "api.log");
        await File.WriteAllLinesAsync(path, Enumerable.Range(1, 5001).Select(index => $"line {index}"));
        IReadOnlyList<string>? lines = null;
        using var reader = new ApiLogFileReader(_directory, watchForChanges: false);
        reader.LinesReset += value => lines = value;

        await reader.SelectFileAsync(path);

        Assert.NotNull(lines);
        Assert.Equal(5000, lines.Count);
        Assert.Equal("line 2", lines[0]);
        Assert.Equal("line 5001", lines[^1]);
    }

    [Fact]
    public async Task DisposeAsync_IsSafeWhileReadIsQueued()
    {
        var path = Path.Combine(_directory, "api.log");
        await File.WriteAllTextAsync(path, "line\n");
        var reader = new ApiLogFileReader(_directory, watchForChanges: false);
        await reader.SelectFileAsync(path);

        var readTask = reader.ReadUpdatesAsync();
        await reader.DisposeAsync();

        await readTask;
        await reader.ReadUpdatesAsync();
    }

    [Fact]
    public async Task ReadUpdatesAsync_ResetsAfterTruncation()
    {
        var path = Path.Combine(_directory, "api.log");
        await File.WriteAllTextAsync(path, "a long original line\n");
        var resets = new List<IReadOnlyList<string>>();
        using var reader = new ApiLogFileReader(_directory, watchForChanges: false);
        reader.LinesReset += lines => resets.Add(lines);

        await reader.SelectFileAsync(path);
        await File.WriteAllTextAsync(path, "new\n");
        await reader.ReadUpdatesAsync();

        Assert.Equal(2, resets.Count);
        Assert.Equal(["new"], resets[1]);
    }

    [Fact]
    public async Task FileWatcher_ResetsWhenSelectedFileIsRenamed()
    {
        var originalPath = Path.Combine(_directory, "api.log");
        var rotatedPath = Path.Combine(_directory, "api.previous.log");
        await File.WriteAllTextAsync(originalPath, "before rotation\n");
        var rotationDetected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IReadOnlyList<string>? resetLines = null;
        using var reader = new ApiLogFileReader(_directory);
        reader.LinesReset += lines => resetLines = lines;
        reader.StatusChanged += status =>
        {
            if (status.Contains("rotated or renamed", StringComparison.OrdinalIgnoreCase))
                rotationDetected.TrySetResult();
        };
        await reader.InitializeAsync();
        await reader.SelectFileAsync(originalPath);

        File.Move(originalPath, rotatedPath);
        await rotationDetected.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Empty(resetLines ?? ["not reset"]);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}