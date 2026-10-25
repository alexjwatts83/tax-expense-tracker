using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TaxExpenseTrackerDevLauncher.Models;
using TaxExpenseTrackerDevLauncher.Services;

namespace TaxExpenseTrackerDevLauncher.Tests;

public sealed class ProcessSupervisorIntegrationTests
{
    [Fact]
    public async Task Constructor_UsesDefaultTimeouts()
    {
        await using var supervisor = new ProcessSupervisor([]);

        Assert.Equal(TimeSpan.FromSeconds(60), supervisor.StartupTimeout);
        Assert.Equal(TimeSpan.FromSeconds(2), supervisor.GracefulStopTimeout);
    }

    [Fact]
    public async Task Constructor_UsesConfiguredTimeouts()
    {
        var settings = new LauncherSettings { StartupTimeoutSeconds = 120, GracefulStopTimeoutSeconds = 7 };
        await using var supervisor = new ProcessSupervisor([], settings);

        Assert.Equal(TimeSpan.FromSeconds(120), supervisor.StartupTimeout);
        Assert.Equal(TimeSpan.FromSeconds(7), supervisor.GracefulStopTimeout);
    }

    [Fact]
    public async Task StartupTimeout_UsesConfiguredLimitAndStopsUnreadyProcess()
    {
        var definition = new ServiceDefinition(
            "test", "Unready service", "powershell.exe",
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
                "[System.Threading.ManualResetEvent]::new($false).WaitOne() | Out-Null"],
            Environment.CurrentDirectory, [], "NEVER_READY", new Uri("http://localhost"));
        await using var supervisor = new ProcessSupervisor([definition], new LauncherSettings { StartupTimeoutSeconds = 1 });
        var timeout = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var crashed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        supervisor.LogReceived += line =>
        {
            if (line.Text.StartsWith("Startup timed out", StringComparison.Ordinal))
                timeout.TrySetResult(line.Text);
        };
        supervisor.StatusChanged += status =>
        {
            if (status.State == ServiceState.Crashed)
                crashed.TrySetResult();
        };

        await supervisor.StartAsync("test");

        Assert.Equal("Startup timed out after 1 seconds.", await timeout.Task.WaitAsync(TimeSpan.FromSeconds(15)));
        await crashed.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task StartAndStop_TransitionsThroughReadinessAndReleasesPort()
    {
        var port = ReserveAvailablePort();
        var script = $"$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, {port}); " +
                     "$listener.Start(); Write-Output 'LAUNCHER_TEST_READY'; " +
                     "while ($true) { Start-Sleep -Milliseconds 100 }";
        var definition = new ServiceDefinition(
            "test",
            "Test service",
            "powershell.exe",
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script],
            Environment.CurrentDirectory,
            [port],
            "LAUNCHER_TEST_READY",
            new Uri($"http://localhost:{port}"));
        await using var supervisor = new ProcessSupervisor([definition]);
        var statuses = new List<ServiceStatusChangedEventArgs>();
        var running = new TaskCompletionSource<ServiceStatusChangedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource<ServiceStatusChangedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        supervisor.StatusChanged += status =>
        {
            lock (statuses)
                statuses.Add(status);

            if (status.State == ServiceState.Running)
                running.TrySetResult(status);
            if (status.State == ServiceState.Stopped)
                stopped.TrySetResult(status);
        };

        await supervisor.StartAsync("test");
        var runningStatus = await running.Task.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.NotNull(runningStatus.ProcessId);
        Assert.Contains(PortInspector.GetOwners([port]), owner => owner.ProcessId == runningStatus.ProcessId);

        await supervisor.StopAsync("test");
        await stopped.Task.WaitAsync(TimeSpan.FromSeconds(10));

        ServiceState[] stateSequence;
        lock (statuses)
            stateSequence = statuses.Select(status => status.State).ToArray();

        Assert.Contains(ServiceState.Starting, stateSequence);
        Assert.Contains(ServiceState.Running, stateSequence);
        Assert.Contains(ServiceState.Stopping, stateSequence);
        Assert.Equal(ServiceState.Stopped, stateSequence[^1]);
        Assert.Empty(PortInspector.GetOwners([port]));
    }

    [Theory]
    [InlineData(200, ServiceState.Running)]
    [InlineData(503, ServiceState.Unhealthy)]
    [InlineData(204, ServiceState.Unhealthy)]
    [InlineData(302, ServiceState.Unhealthy)]
    [InlineData(-1, ServiceState.CertificateError)]
    [InlineData(-2, ServiceState.HealthTimeout)]
    public async Task HealthReadiness_RequiresExactSuccessAndDistinguishesFailures(int responseCode, ServiceState expected)
    {
        var port = ReserveAvailablePort();
        var script = $"$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, {port}); " +
                     "$listener.Start(); Write-Output 'HEALTH_READY'; " +
                     "[System.Threading.ManualResetEvent]::new($false).WaitOne() | Out-Null";
        var healthUri = new Uri($"https://localhost:{port}/health");
        var definition = new ServiceDefinition("test", "Health service", "powershell.exe",
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script],
            Environment.CurrentDirectory, [port], "HEALTH_READY", new Uri($"https://localhost:{port}"), healthUri);
        using var handler = new HealthHandler(responseCode, healthUri, port);
        await using var supervisor = new ProcessSupervisor([definition],
            new LauncherSettings { StartupTimeoutSeconds = 4 }, handler);
        var completed = new TaskCompletionSource<ServiceStatusChangedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        var unexpectedRunning = false;
        supervisor.StatusChanged += status =>
        {
            if (status.State == ServiceState.Running && expected != ServiceState.Running)
                unexpectedRunning = true;
            if (status.State == expected)
                completed.TrySetResult(status);
        };

        await supervisor.StartAsync("test");
        var result = await completed.Task.WaitAsync(TimeSpan.FromSeconds(15));

        Assert.False(unexpectedRunning);
        Assert.True(handler.Requested);
        Assert.NotNull(result.ProcessId);
        await supervisor.StartAsync("test");
        Assert.Contains(PortInspector.GetOwners([port]), owner => owner.ProcessId == result.ProcessId);
        await supervisor.StopAsync("test");
        Assert.Empty(PortInspector.GetOwners([port]));
    }

    [Theory]
    [InlineData(200, false, ServiceState.Running)]
    [InlineData(503, false, ServiceState.Unhealthy)]
    [InlineData(302, false, ServiceState.Unhealthy)]
    [InlineData(200, true, ServiceState.CertificateError)]
    public async Task HealthReadiness_UsesRealHttpAndRejectsUntrustedTls(int responseCode, bool useTls, ServiceState expected)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var healthPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var serverTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var key = RSA.Create(2048);
        var certificateRequest = new CertificateRequest($"CN=Launcher-test-{Guid.NewGuid():N}", key,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(5));
        var serving = ServeAsync();
        var definition = new ServiceDefinition("test", "Loopback health", "powershell.exe",
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
                "Write-Output 'HEALTH_READY'; [System.Threading.ManualResetEvent]::new($false).WaitOne() | Out-Null"],
            Environment.CurrentDirectory, [], "HEALTH_READY", new Uri("http://localhost"),
            new Uri($"{(useTls ? "https" : "http")}://127.0.0.1:{healthPort}/health"));
        await using var supervisor = new ProcessSupervisor([definition], new LauncherSettings { StartupTimeoutSeconds = 8 });
        var completed = new TaskCompletionSource<ServiceState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var diagnostic = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        supervisor.LogReceived += line =>
        {
            if (line.Text.StartsWith("Health check", StringComparison.Ordinal))
                diagnostic.TrySetResult(line.Text);
        };
        supervisor.StatusChanged += status =>
        {
            if (status.State is not ServiceState.Starting)
                completed.TrySetResult(status.State);
        };

        await supervisor.StartAsync("test");

        Assert.Equal(expected, await completed.Task.WaitAsync(TimeSpan.FromSeconds(15)));
        if (useTls)
            Assert.Contains("dotnet dev-certs https --check --trust", await diagnostic.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        else if (responseCode != 200)
            Assert.Contains($"HTTP {responseCode}", await diagnostic.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        await serving;

        async Task ServeAsync()
        {
            using var client = await listener.AcceptTcpClientAsync(serverTimeout.Token);
            await using var network = client.GetStream();
            using var tls = useTls ? new SslStream(network, leaveInnerStreamOpen: true) : null;
            try
            {
                if (tls is not null)
                    await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate = certificate }, serverTimeout.Token);

                var stream = (Stream?)tls ?? network;
                using var reader = new StreamReader(stream, leaveOpen: true);
                Assert.Equal("GET /health HTTP/1.1", await reader.ReadLineAsync(serverTimeout.Token));
                while (!string.IsNullOrEmpty(await reader.ReadLineAsync(serverTimeout.Token))) { }
                var response = Encoding.ASCII.GetBytes($"HTTP/1.1 {responseCode} Test\r\nLocation: http://127.0.0.1:{healthPort}/redirected\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(response, serverTimeout.Token);
            }
            catch (AuthenticationException) when (useTls) { }
            catch (IOException) when (useTls) { }
        }
    }

    [Fact]
    public async Task HealthReadiness_DoesNotProbeBeforeStartupSignal()
    {
        var port = ReserveAvailablePort();
        var definition = new ServiceDefinition("test", "No startup signal", "powershell.exe",
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
                "[System.Threading.ManualResetEvent]::new($false).WaitOne() | Out-Null"],
            Environment.CurrentDirectory, [], "NEVER_READY", new Uri("http://localhost"),
            new Uri($"http://localhost:{port}/health"));
        using var handler = new HealthHandler(200, definition.HealthUri!, port);
        await using var supervisor = new ProcessSupervisor([definition], new LauncherSettings { StartupTimeoutSeconds = 1 }, handler);
        var timedOut = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        supervisor.StatusChanged += status =>
        {
            if (status.State == ServiceState.HealthTimeout)
                timedOut.TrySetResult();
        };

        await supervisor.StartAsync("test");
        await timedOut.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.False(handler.Requested);
    }

    [Fact]
    public async Task StopDuringHealthProbe_CancelsRequestAndNeverReportsRunning()
    {
        var port = ReserveAvailablePort();
        var script = $"$listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, {port}); " +
                     "$listener.Start(); Write-Output 'HEALTH_READY'; " +
                     "[System.Threading.ManualResetEvent]::new($false).WaitOne() | Out-Null";
        var definition = new ServiceDefinition("test", "Cancelled probe", "powershell.exe",
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script],
            Environment.CurrentDirectory, [port], "HEALTH_READY", new Uri("http://localhost"),
            new Uri($"http://localhost:{port}/health"));
        using var handler = new HealthHandler(-2, definition.HealthUri!, port);
        await using var supervisor = new ProcessSupervisor([definition], healthHandler: handler);
        var statuses = new System.Collections.Concurrent.ConcurrentQueue<ServiceState>();
        supervisor.StatusChanged += status => statuses.Enqueue(status.State);

        await supervisor.StartAsync("test");
        await handler.RequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await supervisor.StopAsync("test");
        await handler.RequestCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.DoesNotContain(ServiceState.Running, statuses);
        Assert.DoesNotContain(ServiceState.HealthTimeout, statuses);
        Assert.Equal(ServiceState.Stopped, statuses.Last());
        Assert.Empty(PortInspector.GetOwners([port]));
    }

    private sealed class HealthHandler(int responseCode, Uri healthUri, int port) : HttpMessageHandler
    {
        public bool Requested { get; private set; }
        public TaskCompletionSource RequestStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource RequestCancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(healthUri, request.RequestUri);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.NotEmpty(PortInspector.GetOwners([port]));
            Requested = true;
            RequestStarted.TrySetResult();
            if (responseCode == -1)
                throw new HttpRequestException(HttpRequestError.SecureConnectionError, "Untrusted test certificate");
            if (responseCode == -2)
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                finally
                {
                    if (cancellationToken.IsCancellationRequested)
                        RequestCancelled.TrySetResult();
                }
            }

            return new HttpResponseMessage((HttpStatusCode)responseCode);
        }
    }

    private static int ReserveAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}