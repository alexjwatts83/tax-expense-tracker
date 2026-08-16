using System.Net;
using System.Net.Sockets;
using TaxExpenseTrackerDevLauncher.Models;
using TaxExpenseTrackerDevLauncher.Services;

namespace TaxExpenseTrackerDevLauncher.Tests;

public sealed class LauncherFoundationTests
{
    [Fact]
    public void RepositoryLocator_FindsSolutionAboveNestedDirectory()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "TaxExpenseTracker.sln"), string.Empty);
        var nestedPath = Directory.CreateDirectory(Path.Combine(directory.Path, "one", "two")).FullName;

        var result = RepositoryLocator.FindRepositoryRoot(nestedPath);

        Assert.Equal(directory.Path, result);
    }

    [Fact]
    public void ServiceRegistry_DefinesApiBeforeWebWithExpectedPorts()
    {
        using var directory = new TemporaryDirectory();

        var services = ServiceRegistry.Create(directory.Path);

        var api = Assert.Single(services, service => service.Id == "api");
        var web = Assert.Single(services, service => service.Id == "web");
        Assert.Equal("api", services[0].Id);
        Assert.Equal([7152, 5158], api.Ports);
        Assert.Equal([4200], web.Ports);
        Assert.Equal(Path.Combine(directory.Path, "Frontend"), web.WorkingDirectory);
    }

    [Fact]
    public void ExecutableResolver_ResolvesExtensionlessDotnetCommand()
    {
        var result = ExecutableResolver.Resolve("dotnet");

        Assert.True(Path.IsPathFullyQualified(result));
        Assert.EndsWith("dotnet.exe", result, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(result));
    }

    [Fact]
    public void ApiLogFileLine_ClassifiesErrorAndFatalRows()
    {
        Assert.True(new ApiLogFileLine("2026-08-16|ERROR|failure").IsError);
        Assert.True(new ApiLogFileLine("2026-08-16|FATAL|failure").IsError);
        Assert.False(new ApiLogFileLine("2026-08-16|INFO|ok").IsError);
    }

    [Fact]
    public void PhoneViewport_KnownPhonesIncludeCommonAndroidAndIphoneSizes()
    {
        Assert.Contains(PhoneViewport.KnownPhones, phone => phone.Name == "Samsung Galaxy S24" && phone.Width == 360 && phone.Height == 780);
        Assert.Contains(PhoneViewport.KnownPhones, phone => phone.Name == "Google Pixel 8" && phone.Width == 412 && phone.Height == 915);
        Assert.Contains(PhoneViewport.KnownPhones, phone => phone.Name == "iPhone 15" && phone.Width == 393 && phone.Height == 852);
        Assert.All(PhoneViewport.KnownPhones, phone => Assert.Equal($"{phone.Name} ({phone.Width} x {phone.Height})", phone.DisplayName));
    }

    [Fact]
    public void PortInspector_FindsCurrentProcessListener()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var owner = Assert.Single(PortInspector.GetOwners([port]));

        Assert.Equal(Environment.ProcessId, owner.ProcessId);
        Assert.Equal(port, owner.Port);
    }

    [Fact]
    public void PortInspector_FindsCurrentProcessIpv6Listener()
    {
        using var listener = new TcpListener(IPAddress.IPv6Loopback, 0);
        listener.Server.DualMode = false;
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var owner = Assert.Single(PortInspector.GetOwners([port]));

        Assert.Equal(Environment.ProcessId, owner.ProcessId);
        Assert.Equal(port, owner.Port);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"TaxExpenseTrackerDevLauncher-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}