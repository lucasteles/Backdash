using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Backdash.Core;
using Backdash.Serialization;

namespace Backdash.Network.Client;

interface IUdpClient<in T> : IBackgroundJob, IDisposable where T : struct
{
    public int Port { get; }
    public SocketAddress Address { get; }
    ValueTask<int> SendTo(SocketAddress peerAddress, T payload, CancellationToken ct = default);
    ValueTask<int> SendTo(SocketAddress peerAddress, T payload, Memory<byte> buffer, CancellationToken ct = default);
}

sealed class UdpClient<T> : IUdpClient<T> where T : struct
{
    readonly UdpSocket socket;
    readonly IUdpObserver<T> observer;
    readonly IBinarySerializer<T> serializer;
    readonly Logger logger;
    readonly int maxPacketSize;
    CancellationTokenSource? cancellation;
    public string JobName { get; }
    public int Port => socket.Port;
    public SocketAddress Address => socket.LocalAddress;

    public UdpClient(
        UdpSocket socket,
        IBinarySerializer<T> serializer,
        IUdpObserver<T> observer,
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

    public async Task Start(CancellationToken ct)
    {
        if (cancellation is not null) return;
        cancellation = new();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, cancellation.Token);
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
                if (logger.EnabledLevel is not LogLevel.Off)
                    logger.Write(LogLevel.Error, $"Socket error: {ex}");
                break;
            }
            catch (Exception ex)
            {
                if (logger.EnabledLevel is not LogLevel.Off)
                    logger.Write(LogLevel.Error, $"Socket error: {ex}");
                break;
            }

            if (receivedSize is 0)
                continue;

            serializer.Deserialize(buffer[..receivedSize].Span, ref msg);
            await observer.OnUdpMessage(msg, address, receivedSize, ct).ConfigureAwait(false);
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
