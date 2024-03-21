using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Backdash.Core;
using Backdash.Serialization;

namespace Backdash.Network.Client;

/// <summary>
/// Client for peer communication
/// </summary>
public interface IPeerClient<in T> : IDisposable where T : struct
{
    /// <summary>
    /// Send Message to peer
    /// </summary>
    ValueTask<int> SendTo(SocketAddress peerAddress, T payload, CancellationToken ct = default);

    /// <summary>
    /// Send Message to peer
    /// </summary>
    ValueTask<int> SendTo(SocketAddress peerAddress, T payload, Memory<byte> buffer, CancellationToken ct = default);

    /// <summary>
    /// Start receiving messages
    /// </summary>
    Task StartReceiving(CancellationToken cancellationToken);
}

interface IPeerJobClient<in T> : IBackgroundJob, IPeerClient<T> where T : struct;

sealed class PeerClient<T> : IPeerJobClient<T> where T : struct
{
    readonly IPeerSocket socket;
    readonly IPeerObserver<T> observer;
    readonly IBinarySerializer<T> serializer;
    readonly Logger logger;
    readonly int maxPacketSize;
    CancellationTokenSource? cancellation;
    public string JobName { get; }

    public PeerClient(
        IPeerSocket socket,
        IBinarySerializer<T> serializer,
        IPeerObserver<T> observer,
        Logger logger,
        int maxPacketSize = Max.UdpPacketSize
    )
    {
        ArgumentNullException.ThrowIfNull(socket);
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(logger);

        this.socket = socket;
        this.observer = observer;
        this.serializer = serializer;
        this.logger = logger;
        this.maxPacketSize = maxPacketSize;

        JobName = $"{nameof(UdpClient)} ({socket.Port})";
    }

    public Task Start(CancellationToken cancellationToken) =>
        StartReceiving(cancellationToken);

    public async Task StartReceiving(CancellationToken cancellationToken)
    {
        if (cancellation is not null) return;
        cancellation = new();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellation.Token);
        var token = cts.Token;
        await ReceiveLoop(token).ConfigureAwait(false);
    }

    async Task ReceiveLoop(CancellationToken ct)
    {
        var buffer = Mem.CreatePinnedMemory(maxPacketSize);
        SocketAddress address = new(socket.AddressFamily);
        T msg = default;
        while (!ct.IsCancellationRequested)
        {
            int receivedSize;
            try
            {
                receivedSize = await socket
                    .ReceiveFromAsync(buffer, address, ct)
                    .ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex)
            {
                if (logger.EnabledLevel is not LogLevel.None)
                    logger.Write(LogLevel.Error, $"Socket error: {ex}");
                break;
            }
            catch (Exception ex)
            {
                if (logger.EnabledLevel is not LogLevel.None)
                    logger.Write(LogLevel.Error, $"Socket error: {ex}");
                break;
            }

            if (receivedSize is 0)
                continue;

            try
            {
                serializer.Deserialize(buffer[..receivedSize].Span, ref msg);
                await observer.OnPeerMessage(msg, address, receivedSize, ct).ConfigureAwait(false);
            }
            catch (NetcodeDeserializationException ex)
            {
                logger.Write(LogLevel.Warning, $"UDP Message error: {ex}");
            }
        }

        // ReSharper disable once RedundantAssignment
        buffer = null;
    }

    ValueTask<int> SendTo(
        SocketAddress peerAddress,
        ReadOnlyMemory<byte> payload,
        CancellationToken ct = default
    ) =>
        socket.SendToAsync(payload, peerAddress, ct);

    public async ValueTask<int> SendTo(
        SocketAddress peerAddress,
        T payload,
        Memory<byte> buffer,
        CancellationToken ct = default
    )
    {
        var bodySize = serializer.Serialize(in payload, buffer.Span);
        var sentSize = await SendTo(peerAddress, buffer[..bodySize], ct);
        Trace.Assert(sentSize == bodySize);
        return sentSize;
    }

    public async ValueTask<int> SendTo(
        SocketAddress peerAddress,
        T payload,
        CancellationToken ct = default
    )
    {
        var buffer = ArrayPool<byte>.Shared.Rent(maxPacketSize);
        var sentBytes = await SendTo(peerAddress, payload, buffer, ct);
        ArrayPool<byte>.Shared.Return(buffer);
        return sentBytes;
    }

    public void Dispose()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        socket.Dispose();
    }
}

/// <summary>
/// Create new instances of <see cref="IPeerClient{T}"/>
/// </summary>
public static class PeerClientFactory
{
    internal static IPeerClient<T> Create<T>(
        IPeerSocket socket,
        IBinarySerializer<T> serializer,
        IPeerObserver<T> observer, Logger logger,
        int maxPacketSize = Max.UdpPacketSize
    ) where T : struct =>
        new PeerClient<T>(socket, serializer, observer, logger, maxPacketSize);

    /// <summary>
    ///  Creates new <see cref="IPeerClient{T}"/>
    /// </summary>
    public static IPeerClient<T> Create<T>(
        IPeerSocket socket,
        IPeerObserver<T> observer,
        IBinarySerializer<T>? serializer = null,
        int maxPacketSize = Max.UdpPacketSize
    ) where T : struct =>
        Create(socket,
            serializer ?? BinarySerializerFactory.FindOrThrow<T>(),
            observer,
            Logger.CreateConsoleLogger(LogLevel.None),
            maxPacketSize
        );
}
