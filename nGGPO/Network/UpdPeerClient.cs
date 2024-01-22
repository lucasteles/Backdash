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

public class UdpPeerClient<T>(
    int port,
    IBinarySerializer<T> serializer
) : IDisposable
    where T : struct
{
    uint totalBytesSent;
    public bool LogsEnabled = true;
    readonly Socket socket = CreateSocket(port);
    readonly CancellationTokenSource cancellation = new();
    public int Port => port;
    public uint TotalBytesSent => totalBytesSent;

    public delegate ValueTask OnMessageDelegate(T message, SocketAddress sender,
        CancellationToken stoppingToken);

    readonly Channel<(SocketAddress, T)> sendQueue =
        Channel.CreateUnbounded<(SocketAddress, T)>(
            new()
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = false,
            }
        );

    public event OnMessageDelegate OnMessage = delegate
    {
        return ValueTask.CompletedTask;
    };

    public Task Start(CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken, cancellation.Token);

        return Task.WhenAll(
            StartRead(cts.Token),
            ProcessSendQueue(cts.Token)
        );
    }

    static Socket CreateSocket(int port)
    {
        if (port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
            throw new ArgumentOutOfRangeException(nameof(port));

        Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
        {
            ExclusiveAddressUse = false,
            Blocking = false,
        };

        IPEndPoint localEp = new(IPAddress.Any, port);
        socket.Bind(localEp);
        Tracer.Log("binding udp socket to port {0}.\n", port);
        return socket;
    }

    async Task StartRead(CancellationToken ct)
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

    async Task ProcessSendQueue(CancellationToken ct)
    {
        var buffer = GC.AllocateArray<byte>(
            length: UdpPacketSize,
            pinned: true
        );

        try
        {
            await foreach (var (peerAddress, nextMsg) in sendQueue.Reader.ReadAllAsync(ct))
            {
                var msg = nextMsg;
                var bodySize = serializer.Serialize(ref msg, buffer);
                var memory = MemoryMarshal.CreateFromPinnedArray(buffer, 0, bodySize);
                var sentSize = await SendRawBytes(peerAddress, memory, ct);
                Trace.Assert(sentSize == bodySize);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ChannelClosedException)
        {
        }
    }

    public ValueTask<int> SendRawBytes(
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
        T payload,
        CancellationToken ct = default
    ) =>
        sendQueue.Writer.WriteAsync((peerAddress, payload), ct);

    public void Dispose()
    {
        cancellation.Cancel();
        cancellation.Dispose();
        socket.Dispose();
        sendQueue.Writer.Complete();
    }
}
