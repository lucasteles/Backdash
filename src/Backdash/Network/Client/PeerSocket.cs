using System.Net;
using System.Net.Sockets;

namespace Backdash.Network.Client;

/// <summary>
/// Socket abstraction over peers
/// </summary>
public interface IPeerSocket : IDisposable
{
    /// <inheritdoc cref="Socket.AddressFamily" />
    AddressFamily AddressFamily { get; }

    /// <summary>
    /// Binding port
    /// </summary>
    int Port { get; }

    /// <summary>
    /// Receive bytes from specified remote host
    /// </summary>
    ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketAddress address, CancellationToken cancellationToken);

    /// <summary>
    /// Sends data to the specified remote host.
    /// </summary>
    ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketAddress socketAddress,
        CancellationToken cancellationToken);
}

/// <summary>
/// Factory for peer sockets
/// </summary>
public interface IPeerSocketFactory
{
    /// <summary>
    /// Creates instance of <see cref="IPeerSocket"/>
    /// </summary>
    IPeerSocket Create(int port, RollbackOptions options);
}

sealed class PeerSocketFactory : IPeerSocketFactory
{
    public IPeerSocket Create(int port, RollbackOptions options) => new UdpSocket(port, options.UseIPv6);
}
