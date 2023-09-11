using System.Net;
using System.Net.Sockets;

namespace Backdash.Network.Client;

interface IUdpSocket : IDisposable
{
    public int Port { get; }
    AddressFamily AddressFamily { get; }
    SocketAddress LocalAddress { get; }
    ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketAddress address, CancellationToken ct);
    ValueTask<int> SendToAsync(ReadOnlyMemory<byte> payload, SocketAddress peerAddress, CancellationToken ct);
}

sealed class UdpSocket : IUdpSocket
{
    readonly Socket socket;
    public int Port { get; }

    public SocketAddress LocalAddress { get; }
    public AddressFamily AddressFamily => socket.AddressFamily;

    public UdpSocket(int port)
    {
        Port = port;
        LocalAddress = new IPEndPoint(IPAddress.Loopback, port).Serialize();

        if (port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
            throw new ArgumentOutOfRangeException(nameof(port));

        socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
        {
            ExclusiveAddressUse = false,
            Blocking = false,
        };

        IPEndPoint localEp = new(IPAddress.Any, port);
        socket.Bind(localEp);
    }

    public ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketAddress address, CancellationToken ct) =>
        socket.ReceiveFromAsync(buffer, SocketFlags.None, address, ct);

    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> payload, SocketAddress peerAddress, CancellationToken ct) =>
        socket.SendToAsync(payload, SocketFlags.None, peerAddress, ct);

    public void Dispose() => socket.Dispose();
}
