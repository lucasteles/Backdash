using System.Buffers;
using System.Net;
using System.Net.Sockets;
using nGGPO.Core;
using nGGPO.Serialization;

namespace nGGPO.Network.Client;

interface IUdpClient<in T> : IBackgroundJob, IDisposable where T : struct
{
    public int Port { get; }
    public SocketAddress Address { get; }

    ValueTask<int> SendTo(SocketAddress peerAddress, T payload, CancellationToken ct = default);
    ValueTask<int> SendTo(SocketAddress peerAddress, T payload, byte[] buffer, CancellationToken ct = default);
}

sealed class UdpClient<T> : IUdpClient<T> where T : struct
{
    readonly IUdpSocket socket;
    readonly IUdpObserver<T> observer;
    readonly IBinarySerializer<T> serializer;
    readonly ILogger logger;
    readonly int maxPacketSize;

    CancellationTokenSource? cancellation;
    readonly SemaphoreSlim semaphore = new(1, 1);

    public UdpClient(
        IUdpSocket socket,
        IBinarySerializer<T> serializer,
        IUdpObserver<T> observer,
        ILogger logger,
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

    public string JobName { get; }

    public int Port => socket.Port;
    public SocketAddress Address => socket.LocalAddress;

    public async Task Start(CancellationToken ct)
    {
        if (cancellation is not null)
            return;

        cancellation = new();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, cancellation.Token);
        var token = cts.Token;

        await ReceiveLoop(token).ConfigureAwait(false);
    }

    async Task ReceiveLoop(CancellationToken ct)
    {
        var buffer = Mem.CreatePinnedBuffer(maxPacketSize);
        SocketAddress address = new(socket.AddressFamily);

        while (!ct.IsCancellationRequested)
        {
            int receivedSize;
            try
            {
                receivedSize = await socket
                    .ReceiveFromAsync(buffer, address, ct)
                    .ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
                if (logger.EnabledLevel is not LogLevel.Off)
                    logger.Error(ex, $"Socket error");

                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (receivedSize is 0)
                continue;

            var msg = serializer.Deserialize(buffer.AsSpan()[..receivedSize]);

            await observer.OnUdpMessage(this, msg, address, ct).ConfigureAwait(false);
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
        byte[] buffer,
        CancellationToken ct = default
    )
    {
        var bodySize = serializer.Serialize(ref payload, buffer);
        var sentSize = await SendTo(peerAddress, buffer.AsMemory()[..bodySize], ct);
        Tracer.Assert(sentSize == bodySize);
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
        semaphore.Dispose();
        socket.Dispose();
    }
}
