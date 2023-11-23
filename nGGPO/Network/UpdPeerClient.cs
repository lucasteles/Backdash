using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using nGGPO.Serialization;
using nGGPO.Utils;

namespace nGGPO.Network;

public class UdpPeerClient<T>(
    int port,
    IBinarySerializer<T> serializer
) : IDisposable where T : struct
{
    public const int MaxUdpSize = 65527;
    const int MaxQueuedPackages = 12;

    readonly Socket socket = CreateSocket(port);
    readonly CancellationTokenSource cancellation = new();

    readonly Channel<(SocketAddress, ReadOnlyMemory<byte>)> channel =
        Channel.CreateBounded<(SocketAddress, ReadOnlyMemory<byte>)>(
            new BoundedChannelOptions(MaxQueuedPackages)
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.DropOldest,
            }
        );

    readonly Channel<(SocketAddress, T)> sendQueue = Channel.CreateBounded<(SocketAddress, T)>(
        new BoundedChannelOptions(MaxQueuedPackages)
        {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropOldest,
        }
    );

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
                length: MaxUdpSize,
                pinned: true
            )
            .AsMemory();

        var address = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort).Serialize();

        while (!ct.IsCancellationRequested)
            try
            {
                var receivedSize = await socket
                    .ReceiveFromAsync(buffer, SocketFlags.None, address, ct);

                if (receivedSize is 0) continue;

                await channel.Writer.WriteAsync((address, buffer[..receivedSize]), ct);
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
                length: MaxUdpSize,
                pinned: true
            )
            .AsMemory();

        try
        {
            await foreach (var (peerAddress, msg) in sendQueue.Reader.ReadAllAsync(ct))
            {
                var bodySize = serializer.Serialize(msg, buffer.Span);
                var sentSize = await SendRaw(buffer[..bodySize], peerAddress, ct);
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