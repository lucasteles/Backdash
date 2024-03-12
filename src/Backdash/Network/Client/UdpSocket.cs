using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Backdash.Network.Client;

sealed class UdpSocket : IDisposable
{
    // ReSharper disable InconsistentNaming
    const uint IOC_IN = 0x80000000;
    const uint IOC_VENDOR = 0x18000000;

    const uint SIO_UDP_CONN_RESET = IOC_IN | IOC_VENDOR | 12;

    // ReSharper enable InconsistentNaming
    readonly Socket socket;
    public int Port { get; }
    public SocketAddress LocalAddress { get; }
    public AddressFamily AddressFamily => socket.AddressFamily;

    public UdpSocket(int port, bool useIPv6 = false)
    {
        if (port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
            throw new ArgumentOutOfRangeException(nameof(port));

        Port = port;
        IPEndPoint endpoint = new(useIPv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback, port);
        LocalAddress = endpoint.Serialize();
        socket = new(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
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
        }

        IPEndPoint localEp = new(useIPv6 ? IPAddress.IPv6Any : IPAddress.Any, port);
        socket.Bind(localEp);
    }

    public ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketAddress address, CancellationToken ct) =>
        socket.ReceiveFromAsync(buffer, SocketFlags.None, address, ct);

    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> payload, SocketAddress peerAddress, CancellationToken ct) =>
        socket.SendToAsync(payload, SocketFlags.None, peerAddress, ct);

    public void Dispose() => socket.Dispose();
}
