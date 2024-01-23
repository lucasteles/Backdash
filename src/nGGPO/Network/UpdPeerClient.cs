using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using nGGPO.Serialization;
using nGGPO.Utils;

namespace nGGPO.Network;

using static UdpPeerClient;

static class UdpPeerClient
{
    public const int UdpPacketSize = 65_527;
}

public delegate ValueTask OnMessageDelegate<in T>(
    T message, SocketAddress sender,
    CancellationToken stoppingToken
) where T : struct;

public class UdpPeerClient<T>(
    int port,
    IBinarySerializer<T> serializer
) : IDisposable
    where T : struct
{
    uint totalBytesSent;
    public bool LogsEnabled = true;
    readonly Socket socket = CreateSocket(port);
    CancellationTokenSource? cancellation;
    public int Port => port;
    public uint TotalBytesSent => totalBytesSent;

    readonly Channel<(SocketAddress, T)> sendQueue =
        Channel.CreateUnbounded<(SocketAddress, T)>(
            new()
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = false,
            }
        );

    public event OnMessageDelegate<T>? OnMessage;

    public async Task StartPumping(CancellationToken cancellationToken = default)
    {
        if (cancellation is not null)
            return;

        cancellation = new();

        using var cts = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken, cancellation.Token);

        await Task.WhenAll(
            StartRead(cts.Token).AsTask(),
            ProcessSendQueue(cts.Token).AsTask()
        );
    }

    public async ValueTask StopPumping()
    {
        if (cancellation is null)
            return;

        await cancellation.CancelAsync();
        cancellation = null;
    }

    static Socket CreateSocket(int port)
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
        Tracer.Log("binding udp socket to port {0}.\n", port);
        return newSocket;
    }

    async ValueTask StartRead(CancellationToken ct)
    {
        var buffer = GC.AllocateArray<byte>(
            length: UdpPacketSize,
            pinned: true
        );

        SocketAddress address = new(socket.AddressFamily);

        while (!ct.IsCancellationRequested)
            try
            {
                var receivedSize = await socket
                    .ReceiveFromAsync(buffer, SocketFlags.None, address, ct);

                if (receivedSize is 0)
                    continue;

                var memory = MemoryMarshal.CreateFromPinnedArray(buffer, 0, receivedSize);
                var parsed = serializer.Deserialize(memory.Span);

                if (OnMessage is not null)
                    await OnMessage.Invoke(parsed, address, ct);
            }
            catch (SocketException ex)
            {
                if (LogsEnabled)
                    Tracer.Warn(ex, "Socket error");

                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
    }

    async ValueTask ProcessSendQueue(CancellationToken ct)
    {
        var sendBuffer = GC.AllocateArray<byte>(
            length: UdpPacketSize,
            pinned: true
        );

        try
        {
            await foreach (var (peerAddress, nextMsg) in sendQueue.Reader.ReadAllAsync(ct))
            {
                var msg = nextMsg;
                var bodySize = serializer.Serialize(ref msg, sendBuffer);
                var memory = MemoryMarshal.CreateFromPinnedArray(sendBuffer, 0, bodySize);
                var sentSize = await SendBytes(peerAddress, memory, ct);
                Trace.Assert(sentSize == bodySize);
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
        totalBytesSent += (uint)payload.Length;
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
        if (cancellation is not null)
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }

        socket.Dispose();
        sendQueue.Writer.Complete();
    }
}
