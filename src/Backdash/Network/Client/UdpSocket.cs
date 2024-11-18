using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Core;

namespace Backdash.Network.Client;

/// <summary>
/// UDP specialized socket interface.
/// </summary>
public sealed class UdpSocket : IPeerSocket
{
    // ReSharper disable InconsistentNaming
    const uint IOC_IN = 0x80000000;
    const uint IOC_VENDOR = 0x18000000;

    const uint SIO_UDP_CONN_RESET = IOC_IN | IOC_VENDOR | 12;

    readonly Socket socket;
    readonly IPEndPoint anyEndPoint;

    /// <summary>
    /// Gets the main bind port of the Socket.
    /// </summary>
    public int Port { get; }

    /// <inheritdoc cref="Socket.AddressFamily" />
    public AddressFamily AddressFamily => socket.AddressFamily;

    /// <summary>
    /// Initialize and bind a new <see cref="UdpSocket"/>.
    /// </summary>
    /// <param name="bindEndpoint">Local socket binding.</param>
    /// <exception cref="NetcodeException">Throws if the <see cref="AddressFamily"/> of <see cref="IPAddress"/> in <paramref name="bindEndpoint"/>
    /// is not <see cref="System.Net.Sockets.AddressFamily.InterNetwork"/> or <see cref="System.Net.Sockets.AddressFamily.InterNetworkV6"/>> </exception>
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
            socket.LingerState = new(false, 0);
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


    /// <inheritdoc  />
    public UdpSocket(IPAddress bindAddress, int port) : this(new(bindAddress, port)) { }

    /// <inheritdoc  />
    public UdpSocket(int port, bool useIPv6 = false) : this(useIPv6 ? IPAddress.IPv6Any : IPAddress.Any, port) { }

    /// <inheritdoc  />
    public UdpSocket(string bindHost, int port, AddressFamily addressFamily = AddressFamily.InterNetwork)
        : this(GetDnsIpAddress(bindHost, addressFamily), port) { }

    /// <summary>
    /// Returns the Internet Protocol (IP) addresses for the specified host and <see cref="AddressFamily"/>.
    /// </summary>
    /// <exception cref="NetcodeException"></exception>
    public static IPAddress GetDnsIpAddress(string host, AddressFamily addressFamily = AddressFamily.InterNetwork)
    {
        var address = Dns.GetHostAddresses(host, addressFamily).FirstOrDefault()
                      ?? throw new NetcodeException($"Unable to retrieve IP Address from host {host}");
        return address;
    }

    /// <summary>
    /// Receives a datagram into the data buffer, using the specified SocketFlags, and stores the endpoint.
    /// </summary>
    /// <param name="buffer">The buffer for the received data.</param>
    /// <param name="address"> A <see cref="SocketAddress "/> instance that gets updated with the value of the remote peer when this method returns.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to signal the asynchronous operation should be canceled.</param>
    /// <returns>An asynchronous task that completes with a <see cref="SocketReceiveFromResult"/> containing the number of bytes received and the endpoint of the sending host.</returns>
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    public ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketAddress address,
        CancellationToken cancellationToken) =>
        socket.ReceiveFromAsync(buffer, SocketFlags.None, address, cancellationToken);

    /// <summary>
    /// Receives data and returns the endpoint of the sending host.
    /// </summary>
    /// <param name="buffer">The buffer for the received data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to signal the asynchronous operation should be canceled.</param>
    /// <returns>An asynchronous task that completes with a <see cref="SocketReceiveFromResult"/> containing the number of bytes received and the endpoint of the sending host.</returns>
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    public ValueTask<SocketReceiveFromResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
        socket.ReceiveFromAsync(buffer, SocketFlags.None, anyEndPoint, cancellationToken);

    /// <summary>
    /// Sends data to the specified remote host.
    /// </summary>
    /// <param name="buffer">The buffer for the data to send.</param>
    /// <param name="socketAddress">The remote host to which to send the data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous task that completes with the number of bytes sent.</returns>
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketAddress socketAddress,
        CancellationToken cancellationToken) =>
        socket.SendToAsync(buffer, SocketFlags.None, socketAddress, cancellationToken);

    /// <summary>
    /// Sends data to the specified remote host.
    /// </summary>
    /// <param name="buffer">The buffer for the data to send.</param>
    /// <param name="remoteEndPoint">The remote host to which to send the data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous task that completes with the number of bytes sent.</returns>
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, EndPoint remoteEndPoint,
        CancellationToken cancellationToken) =>
        socket.SendToAsync(buffer, SocketFlags.None, remoteEndPoint, cancellationToken);

    /// <inheritdoc  />
    public void Dispose() => socket.Dispose();

    /// <inheritdoc cref="Socket.Close()" />
    public void Close() => socket.Close();
}
