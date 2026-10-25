using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TaxExpenseTrackerDevLauncher.Services;

public sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activation;
    private readonly RegisteredWaitHandle? _listener;

    public SingleInstanceCoordinator(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryRoot)).ToUpperInvariant();
        var repositoryKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedRoot)));
        var instanceName = $@"Local\TaxExpenseTrackerDevLauncher-{repositoryKey}";

        _activation = new EventWaitHandle(false, EventResetMode.AutoReset, $"{instanceName}-activate");
        _mutex = new Mutex(false, instanceName, out var createdNew);
        IsPrimary = createdNew;
        if (IsPrimary)
            _listener = ThreadPool.RegisterWaitForSingleObject(
                _activation, (_, _) => ActivationRequested?.Invoke(), null, Timeout.Infinite, false);
    }

    public bool IsPrimary { get; }

    public event Action? ActivationRequested;

    public void RequestActivation() => _activation.Set();

    public void Dispose()
    {
        _listener?.Unregister(null);
        _activation.Dispose();
        _mutex.Dispose();
    }
}