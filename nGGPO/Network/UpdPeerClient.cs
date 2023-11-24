using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using nGGPO.Serialization;
using nGGPO.Utils;

namespace nGGPO.Network;

using static UdpPeerClient;

static class UdpPeerClient
{
    const int MaxQueuedPackages = 60;

    public static readonly BoundedChannelOptions ChannelOptions =
        new(MaxQueuedPackages)
        {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropOldest,
        };
}

public class UdpPeerClient<T>(
    int port,
    IBinarySerializer<T> serializer
) : IDisposable where T : struct
{
    public const int UdpPacketSize = 65_527;

    readonly Socket socket = CreateSocket(port);
    readonly CancellationTokenSource cancellation = new();

    readonly Channel<(SocketAddress, ReadOnlyMemory<byte>)> channel =
        Channel.CreateBounded<(SocketAddress, ReadOnlyMemory<byte>)>(ChannelOptions);

    readonly Channel<(SocketAddress, T)> sendQueue =
        Channel.CreateBounded<(SocketAddress, T)>(ChannelOptions);

    public event Func<T, SocketAddress, CancellationToken, ValueTask> OnMessage = delegate
    {
        return ValueTask.CompletedTask;
    };

    public int Port => port;

    public async Task Start(CancellationToken cancellationToken = default)
    {
        var cts = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken, cancellation.Token);

        await Task.WhenAll(
            Produce(cts.Token),
            Consume(cts.Token),
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

    async Task Produce(CancellationToken ct)
    {
        var buffer = GC.AllocateArray<byte>(
            length: UdpPacketSize,
            pinned: true
        );

        var address = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort).Serialize();

        while (!ct.IsCancellationRequested)
            try
            {
                var receivedSize = await socket
                    .ReceiveFromAsync(buffer, SocketFlags.None, address, ct);

                if (receivedSize is 0) continue;

                var memory = MemoryMarshal.CreateFromPinnedArray(buffer, 0, receivedSize);
                await channel.Writer.WriteAsync((address, memory), ct);
            }
            catch (SocketException ex)
            {
                await Console.Out.WriteLineAsync($"Socket error: {ex}");
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }

        channel.Writer.Complete();
    }

    async Task Consume(CancellationToken ct)
    {
        try
        {
            await foreach (var (peerAddress, pkg) in channel.Reader.ReadAllAsync(ct))
            {
                var parsed = serializer.Deserialize(pkg.Span);
                await OnMessage.Invoke(parsed, peerAddress, ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ChannelClosedException)
        {
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
                var sentSize = await SendRaw(memory, peerAddress, ct);
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

    public async ValueTask<int> SendRaw(
        ReadOnlyMemory<byte> payload,
        SocketAddress peerAddress,
        CancellationToken ct = default
    ) =>
        await socket.SendToAsync(payload, SocketFlags.None, peerAddress, ct);

    public ValueTask SendTo(T payload, IPEndPoint dest, CancellationToken ct = default) =>
        SendTo(payload, dest.Serialize(), ct);

    public ValueTask SendTo(T payload, SocketAddress peerAddress, CancellationToken ct = default) =>
        sendQueue.Writer.WriteAsync((peerAddress, payload), ct);

    public void Dispose()
    {
        cancellation.Cancel();
        cancellation.Dispose();
        socket.Dispose();
        sendQueue.Writer.Complete();
    }
}