using System.IO;
using System.Text;
using TaxExpenseTrackerDevLauncher.Models;

namespace TaxExpenseTrackerDevLauncher.Services;

public sealed class ApiLogFileReader : IDisposable, IAsyncDisposable
{
    public const string DefaultLogDirectory = @"C:\logs\TaxExpenseTracker.Api";
    private const int MaximumLines = 5000;
    private const int TailScanBufferSize = 64 * 1024;
    private readonly SemaphoreSlim _readGate = new(1, 1);
    private readonly bool _watchForChanges;
    private FileSystemWatcher? _watcher;
    private string? _selectedPath;
    private string _pendingText = string.Empty;
    private long _position;
    private bool _disposed;

    public ApiLogFileReader(string? logDirectory = null, bool watchForChanges = true)
    {
        LogDirectory = logDirectory ?? DefaultLogDirectory;
        _watchForChanges = watchForChanges;
    }

    public string LogDirectory { get; }

    public event Action<IReadOnlyList<ApiLogFileInfo>>? FilesChanged;
    public event Action<IReadOnlyList<string>>? LinesReset;
    public event Action<IReadOnlyList<string>>? LinesAppended;
    public event Action<string>? StatusChanged;

    public Task InitializeAsync() => RefreshFilesAsync();

    public Task ReadUpdatesAsync() => ReadAppendedLinesAsync();

    public async Task RefreshFilesAsync()
    {
        if (_disposed)
            return;

        try
        {
            if (!Directory.Exists(LogDirectory))
            {
                DisposeWatcher();
                FilesChanged?.Invoke([]);
                StatusChanged?.Invoke($"Log folder does not exist: {LogDirectory}");
                return;
            }

            EnsureWatcher();
            var files = Directory.EnumerateFiles(LogDirectory, "*.log", SearchOption.TopDirectoryOnly)
                .Select(path => new ApiLogFileInfo(path, File.GetLastWriteTime(path)))
                .OrderByDescending(file => file.LastWriteTime)
                .ToArray();

            FilesChanged?.Invoke(files);
            StatusChanged?.Invoke(files.Length == 0
                ? "No API log files found."
                : $"{files.Length} API log file{(files.Length == 1 ? string.Empty : "s")} found.");
        }
        catch (UnauthorizedAccessException exception)
        {
            FilesChanged?.Invoke([]);
            StatusChanged?.Invoke($"Access denied: {exception.Message}");
        }
        catch (IOException exception)
        {
            StatusChanged?.Invoke($"Could not refresh API log files: {exception.Message}");
        }

        await Task.CompletedTask;
    }

    public async Task SelectFileAsync(string? path)
    {
        await _readGate.WaitAsync();
        try
        {
            if (_disposed)
                return;

            _selectedPath = path;
            _position = 0;
            _pendingText = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                LinesReset?.Invoke([]);
                return;
            }

            await ReadSelectedFileAsync(reset: true);
        }
        finally
        {
            _readGate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        DisposeWatcher();
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await _readGate.WaitAsync();
        _readGate.Release();
    }

    private void EnsureWatcher()
    {
        if (!_watchForChanges || _watcher is not null)
            return;

        _watcher = new FileSystemWatcher(LogDirectory, "*.log")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFilesChanged;
        _watcher.Deleted += OnFilesChanged;
        _watcher.Renamed += OnFileRenamed;
        _watcher.Error += OnWatcherError;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs args)
    {
        if (PathsEqual(args.FullPath, _selectedPath))
            _ = ReadAppendedLinesAsync();
    }

    private void OnFilesChanged(object sender, FileSystemEventArgs args) => _ = RefreshFilesAsync();

    private void OnFileRenamed(object sender, RenamedEventArgs args)
    {
        if (PathsEqual(args.OldFullPath, _selectedPath))
        {
            _selectedPath = null;
            _position = 0;
            LinesReset?.Invoke([]);
            StatusChanged?.Invoke("The selected API log file was rotated or renamed.");
        }

        _ = RefreshFilesAsync();
    }

    private void OnWatcherError(object sender, ErrorEventArgs args)
    {
        StatusChanged?.Invoke($"API log watcher error: {args.GetException().Message}");
        DisposeWatcher();
        _ = RefreshFilesAsync();
    }

    private async Task ReadAppendedLinesAsync()
    {
        if (_disposed)
            return;

        await _readGate.WaitAsync();
        try
        {
            if (_disposed)
                return;

            await ReadSelectedFileAsync(reset: false);
        }
        finally
        {
            _readGate.Release();
        }
    }

    private async Task ReadSelectedFileAsync(bool reset)
    {
        var path = _selectedPath;
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                useAsync: true);

            if (stream.Length < _position)
            {
                _position = 0;
                _pendingText = string.Empty;
                reset = true;
            }

            if (reset)
            {
                _position = await FindTailStartAsync(stream);
                _pendingText = string.Empty;
            }

            stream.Seek(_position, SeekOrigin.Begin);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: _position == 0, leaveOpen: true);
            var content = await reader.ReadToEndAsync();
            _position = stream.Position;
            var lines = ExtractCompleteLines(content);

            if (reset)
                LinesReset?.Invoke(lines);
            else if (lines.Count > 0)
                LinesAppended?.Invoke(lines);

            StatusChanged?.Invoke($"Following {Path.GetFileName(path)}");
        }
        catch (FileNotFoundException)
        {
            _position = 0;
            _pendingText = string.Empty;
            LinesReset?.Invoke([]);
            StatusChanged?.Invoke("The selected API log file no longer exists.");
            await RefreshFilesAsync();
        }
        catch (UnauthorizedAccessException exception)
        {
            StatusChanged?.Invoke($"Access denied: {exception.Message}");
        }
        catch (IOException exception)
        {
            StatusChanged?.Invoke($"Could not read API log file: {exception.Message}");
        }
    }

    private async Task<long> FindTailStartAsync(FileStream stream)
    {
        if (stream.Length == 0)
            return 0;

        stream.Seek(-1, SeekOrigin.End);
        var endsWithNewLine = stream.ReadByte() == '\n';
        var newLinesNeeded = MaximumLines + (endsWithNewLine ? 1 : 0);
        var buffer = new byte[TailScanBufferSize];
        var position = stream.Length;
        var newLinesFound = 0;

        while (position > 0)
        {
            var bytesToRead = (int)Math.Min(buffer.Length, position);
            position -= bytesToRead;
            stream.Seek(position, SeekOrigin.Begin);
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, bytesToRead));

            for (var index = bytesRead - 1; index >= 0; index--)
            {
                if (buffer[index] != '\n')
                    continue;

                newLinesFound++;
                if (newLinesFound >= newLinesNeeded)
                    return position + index + 1;
            }
        }

        return 0;
    }

    private IReadOnlyList<string> ExtractCompleteLines(string content)
    {
        var combined = _pendingText + content;
        var lastNewLine = combined.LastIndexOf('\n');
        if (lastNewLine < 0)
        {
            _pendingText = combined;
            return [];
        }

        _pendingText = combined[(lastNewLine + 1)..];
        return combined[..lastNewLine]
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 0)
            .TakeLast(MaximumLines)
            .ToArray();
    }

    private void DisposeWatcher()
    {
        if (_watcher is null)
            return;

        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _watcher = null;
    }

    private static bool PathsEqual(string? left, string? right) =>
        left is not null && right is not null &&
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
}