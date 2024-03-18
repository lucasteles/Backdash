using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Backdash.Core;

namespace Backdash.Network.Client;

public sealed class UdpSocket : IDisposable
{
    // ReSharper disable InconsistentNaming
    const uint IOC_IN = 0x80000000;
    const uint IOC_VENDOR = 0x18000000;

    const uint SIO_UDP_CONN_RESET = IOC_IN | IOC_VENDOR | 12;

    // ReSharper enable InconsistentNaming
    readonly Socket socket;
    readonly IPEndPoint anyEndPoint;
    public int Port { get; }

    public AddressFamily AddressFamily => socket.AddressFamily;

    public UdpSocket(IPEndPoint bindEndpoint)
    {
        anyEndPoint = new(bindEndpoint.AddressFamily switch
        {
            AddressFamily.InterNetwork => IPAddress.Any,
            AddressFamily.InterNetworkV6 => IPAddress.IPv6Any,
            _ => throw new NetcodeException($"Invalid binding endpoint address family {bindEndpoint.AddressFamily}"),
        }, IPEndPoint.MinPort);

        Port = bindEndpoint.Port;

        socket = new(bindEndpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            ExclusiveAddressUse = false,
            Blocking = false,
        };
        socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        try
        {
            socket.LingerState = new LingerOption(false, 0);
        }
        catch
        {
            // skip
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            socket.IOControl((IOControlCode)SIO_UDP_CONN_RESET, [0, 0, 0, 0,], null);
            socket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
        }

        socket.Bind(bindEndpoint);
    }

    public UdpSocket(IPAddress bindAddress, int port) : this(new IPEndPoint(bindAddress, port)) { }
    public UdpSocket(int port, bool useIPv6 = false) : this(useIPv6 ? IPAddress.IPv6Any : IPAddress.Any, port) { }

    public UdpSocket(string bindHost, int port, AddressFamily addressFamily = AddressFamily.InterNetwork)
        : this(GetDnsIpAddress(bindHost, addressFamily), port) { }

    public static IPAddress GetDnsIpAddress(string host, AddressFamily addressFamily = AddressFamily.InterNetwork)
    {
        var address = Dns.GetHostAddresses(host, addressFamily).FirstOrDefault()
                      ?? throw new NetcodeException($"Unable to retrieve IP Address from host {host}");
        return address;
    }

    public ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketAddress address, CancellationToken ct) =>
        socket.ReceiveFromAsync(buffer, SocketFlags.None, address, ct);

    public ValueTask<SocketReceiveFromResult> ReceiveAsync(Memory<byte> buffer, CancellationToken ct) =>
        socket.ReceiveFromAsync(buffer, SocketFlags.None, anyEndPoint, ct);

    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> payload, SocketAddress peerAddress, CancellationToken ct) =>
        socket.SendToAsync(payload, SocketFlags.None, peerAddress, ct);

    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> payload, EndPoint peerAddress, CancellationToken ct) =>
        socket.SendToAsync(payload, SocketFlags.None, peerAddress, ct);

    public void Dispose() => socket.Dispose();

    public void Close() => socket.Close();
}
