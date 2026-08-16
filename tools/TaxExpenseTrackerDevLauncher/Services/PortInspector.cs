using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TaxExpenseTrackerDevLauncher.Models;

namespace TaxExpenseTrackerDevLauncher.Services;

public static class PortInspector
{
    private const int AddressFamilyInterNetwork = 2;
    private const int AddressFamilyInterNetworkV6 = 23;
    private const int InsufficientBuffer = 122;
    private static readonly TimeSpan ProcessExitTimeout = TimeSpan.FromSeconds(5);

    public static IReadOnlyList<PortOwner> GetOwners(IEnumerable<int> ports)
    {
        var requestedPorts = ports.ToHashSet();
        if (requestedPorts.Count == 0)
            return [];

        return GetOwners(requestedPorts, AddressFamilyInterNetwork)
            .Concat(GetOwners(requestedPorts, AddressFamilyInterNetworkV6))
            .DistinctBy(owner => (owner.Port, owner.ProcessId))
            .OrderBy(owner => owner.Port)
            .ToArray();
    }

    public static async Task KillProcessTreeAsync(PortOwner owner, CancellationToken cancellationToken = default)
    {
        Process process;
        try
        {
            process = Process.GetProcessById(owner.ProcessId);
        }
        catch (ArgumentException)
        {
            return;
        }

        using (process)
        {
            DateTimeOffset processStartedAt;
            try
            {
                if (process.HasExited)
                    return;

                processStartedAt = new DateTimeOffset(process.StartTime);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            if (owner.ProcessStartedAt is null || processStartedAt != owner.ProcessStartedAt)
                throw new InvalidOperationException($"Process {owner.ProcessId} no longer matches the confirmed port owner.");

            try
            {
                process.Kill(entireProcessTree: true);
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(ProcessExitTimeout);
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Process {owner.ProcessId} did not exit within {ProcessExitTimeout.TotalSeconds:0} seconds.");
            }
            catch (InvalidOperationException)
            {
                return;
            }
        }
    }

    private static IReadOnlyList<PortOwner> GetOwners(IReadOnlySet<int> requestedPorts, int addressFamily)
    {
        var bufferSize = 0;
        var result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, addressFamily, TcpTableClass.OwnerPidListener, 0);
        if (result == 0 && bufferSize == 0)
            return [];

        if (result != InsufficientBuffer)
            throw new Win32Exception(result, $"Could not inspect TCP listeners for address family {addressFamily}.");

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = GetExtendedTcpTable(buffer, ref bufferSize, true, addressFamily, TcpTableClass.OwnerPidListener, 0);
            if (result != 0)
                throw new Win32Exception(result, $"Could not inspect TCP listeners for address family {addressFamily}.");

            var rowCount = Marshal.ReadInt32(buffer);
            var rowPointer = IntPtr.Add(buffer, sizeof(int));
            var rowSize = addressFamily == AddressFamilyInterNetwork
                ? Marshal.SizeOf<TcpRowOwnerPid>()
                : Marshal.SizeOf<Tcp6RowOwnerPid>();
            var owners = new List<PortOwner>();

            for (var index = 0; index < rowCount; index++)
            {
                var currentRow = IntPtr.Add(rowPointer, index * rowSize);
                var localPort = addressFamily == AddressFamilyInterNetwork
                    ? Marshal.PtrToStructure<TcpRowOwnerPid>(currentRow).LocalPort
                    : Marshal.PtrToStructure<Tcp6RowOwnerPid>(currentRow).LocalPort;
                var owningProcessId = addressFamily == AddressFamilyInterNetwork
                    ? Marshal.PtrToStructure<TcpRowOwnerPid>(currentRow).OwningProcessId
                    : Marshal.PtrToStructure<Tcp6RowOwnerPid>(currentRow).OwningProcessId;
                var port = (ushort)System.Net.IPAddress.NetworkToHostOrder((short)localPort);
                if (!requestedPorts.Contains(port))
                    continue;

                var processId = checked((int)owningProcessId);
                var processDetails = GetProcessDetails(processId);
                owners.Add(new PortOwner(port, processId, processDetails.Name, processDetails.StartedAt));
            }

            return owners;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static (string Name, DateTimeOffset? StartedAt) GetProcessDetails(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return (process.ProcessName, new DateTimeOffset(process.StartTime));
        }
        catch
        {
            return ("unknown", null);
        }
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern int GetExtendedTcpTable(
        IntPtr tcpTable,
        ref int size,
        bool order,
        int addressFamily,
        TcpTableClass tableClass,
        uint reserved);

    [StructLayout(LayoutKind.Sequential)]
    private struct TcpRowOwnerPid
    {
        public uint State;
        public uint LocalAddress;
        public uint LocalPort;
        public uint RemoteAddress;
        public uint RemotePort;
        public uint OwningProcessId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Tcp6RowOwnerPid
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] LocalAddress;
        public uint LocalScopeId;
        public uint LocalPort;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] RemoteAddress;
        public uint RemoteScopeId;
        public uint RemotePort;
        public uint State;
        public uint OwningProcessId;
    }

    private enum TcpTableClass
    {
        OwnerPidListener = 3
    }
}