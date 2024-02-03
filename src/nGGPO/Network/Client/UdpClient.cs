using System.Net;
using System.Net.Sockets;
using nGGPO.Core;
using nGGPO.Data;
using nGGPO.Serialization;

namespace nGGPO.Network.Client;

interface IUdpClient<in T> : IBackgroundJob, IDisposable where T : struct
{
    public int Port { get; }
    public SocketAddress Address { get; }
    public ByteSize TotalBytesSent { get; }

    ValueTask SendTo(SocketAddress peerAddress, T payload, CancellationToken ct = default);
}

sealed class UdpClient<T> : IUdpClient<T> where T : struct
{
    readonly IUdpSocket socket;
    readonly IUdpObserver<T> observer;
    readonly IBinarySerializer<T> serializer;
    readonly ILogger logger;
    readonly int maxPacketSize;

    CancellationTokenSource? cancellation;
    byte[] sendBuffer;

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
        sendBuffer = CreateBuffer();
        JobName = $"{nameof(UdpClient)} ({socket.Port})";
    }

    public ByteSize TotalBytesSent { get; private set; }

    public string JobName { get; }

    public int Port => socket.Port;
    public SocketAddress Address => socket.LocalAddress;

    byte[] CreateBuffer() => GC.AllocateArray<byte>(length: maxPacketSize, pinned: true);

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
        var buffer = GC.AllocateArray<byte>(length: maxPacketSize, pinned: true);
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

    async ValueTask<int> SendBytes(
        SocketAddress peerAddress,
        ReadOnlyMemory<byte> payload,
        CancellationToken ct = default
    )
    {
        ByteSize payloadSize = new(payload.Length);
        TotalBytesSent += payloadSize;
        return await socket.SendToAsync(payload, peerAddress, ct);
    }

    public async ValueTask SendTo(
        SocketAddress peerAddress,
        T payload,
        CancellationToken ct = default
    )
    {
        var bodySize = serializer.Serialize(ref payload, sendBuffer);
        var sentSize = await SendBytes(peerAddress, sendBuffer.AsMemory()[..bodySize], ct);
        Tracer.Assert(sentSize == bodySize);
    }

    public void Dispose()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        sendBuffer = null!;
        socket.Dispose();
    }
}
