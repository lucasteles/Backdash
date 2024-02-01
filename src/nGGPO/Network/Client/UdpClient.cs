using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using nGGPO.Data;
using nGGPO.Lifecycle;
using nGGPO.Serialization;
using nGGPO.Utils;

namespace nGGPO.Network.Client;

interface IUdpClient<T> : IBackgroundJob, IDisposable where T : struct
{
    public int Port { get; }
    public SocketAddress Address { get; }
    public ByteSize TotalBytesSent { get; }

    ValueTask SendTo(
        SocketAddress peerAddress,
        in T payload,
        CancellationToken ct = default
    );
}

sealed class UdpClient<T>(
    int port,
    IUdpObserver<T> observer,
    IBinarySerializer<T> serializer,
    ILogger logger
) : IUdpClient<T> where T : struct
{
    public bool LogsEnabled = true;
    readonly Socket socket = CreateSocket(port, logger);
    CancellationTokenSource? cancellation;
    public int Port => port;
    public SocketAddress Address { get; } = new IPEndPoint(IPAddress.Loopback, port).Serialize();
    public ByteSize TotalBytesSent { get; private set; }

    readonly Channel<(SocketAddress Address, T Payload)> sendQueue =
        Channel.CreateUnbounded<(SocketAddress, T)>(
            new()
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = true,
            }
        );

    public string JobName { get; } = $"{nameof(UdpClient)} ({port})";

    public async Task Start(CancellationToken ct)
    {
        if (cancellation is not null)
            return;

        cancellation = new();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, cancellation.Token);
        var token = cts.Token;

        await Task.WhenAll(
            Task.Run(() => ReadLoop(token), token),
            Task.Run(() => SendLoop(token), token)
        ).ConfigureAwait(false);
    }

    static Socket CreateSocket(int port, ILogger logger)
    {
        if (port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
            throw new ArgumentOutOfRangeException(nameof(port));

        Socket newSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
        {
            ExclusiveAddressUse = false,
            Blocking = false,
        };

        IPEndPoint localEp = new(IPAddress.Any, port);
        newSocket.Bind(localEp);
        logger.Info($"binding udp socket to port {port}.\n");

        return newSocket;
    }

    async Task ReadLoop(CancellationToken ct)
    {
        var bufferArray = GC.AllocateArray<byte>(
            length: Max.UdpPacketSize,
            pinned: true
        );
        var buffer = MemoryMarshal.CreateFromPinnedArray(bufferArray, 0, bufferArray.Length);

        SocketAddress address = new(socket.AddressFamily);

        while (!ct.IsCancellationRequested)
        {
            int receivedSize;
            try
            {
                receivedSize = await socket
                    .ReceiveFromAsync(buffer, SocketFlags.None, address, ct)
                    .ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
                if (LogsEnabled)
                    logger.Error(ex, $"Socket error");

                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (receivedSize is 0)
                continue;

            var msg = serializer.Deserialize(buffer.Span[..receivedSize]);

            await observer.OnUdpMessage(this, msg, address, ct).ConfigureAwait(false);
        }
    }

    async Task SendLoop(CancellationToken ct)
    {
        var bufferArray = GC.AllocateArray<byte>(
            length: Max.UdpPacketSize,
            pinned: true
        );
        var sendBuffer = MemoryMarshal.CreateFromPinnedArray(bufferArray, 0, bufferArray.Length);
        var reader = sendQueue.Reader;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                // await reader.WaitToReadAsync(ct).ConfigureAwait(false);

                while (reader.TryRead(out var msg))
                {
                    if (ct.IsCancellationRequested) break;
                    var bodySize = serializer.Serialize(ref msg.Payload, sendBuffer.Span);
                    var sentSize = await SendBytes(msg.Address, sendBuffer[..bodySize], ct).ConfigureAwait(false);
                    Tracer.Assert(sentSize == bodySize);
                }

                await Task.Yield();
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore when cancelled
        }
        catch (ChannelClosedException)
        {
            // Ignore when channel closed
        }
    }

    ValueTask<int> SendBytes(
        SocketAddress peerAddress,
        ReadOnlyMemory<byte> payload,
        CancellationToken ct = default
    )
    {
        ByteSize payloadSize = new(payload.Length);
        TotalBytesSent += payloadSize;
        return socket.SendToAsync(payload, SocketFlags.None, peerAddress, ct);
    }

    public ValueTask SendTo(
        SocketAddress peerAddress,
        in T payload,
        CancellationToken ct = default
    ) =>
        sendQueue.Writer.WriteAsync((peerAddress, payload), ct);

    public void Dispose()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        socket.Dispose();
        sendQueue.Writer.Complete();
    }
}