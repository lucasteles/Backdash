using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using nGGPO.Serialization;

namespace nGGPO.Network;

public sealed class PeerUdpClient<T>(
    int port,
    IPEndPoint peer,
    IBinarySerializer2<T> serializer
) : IDisposable where T : struct
{
    public const int MaxUdpSize = 65527;
    const int MaxQueuedPackages = 12;

    readonly Socket socket = CreateSocket(port);
    readonly CancellationTokenSource cancellation = new();
    readonly SocketAddress peerAddress = peer.Serialize();

    readonly Channel<ReadOnlyMemory<byte>> channel =
        Channel.CreateBounded<ReadOnlyMemory<byte>>(
            new BoundedChannelOptions(MaxQueuedPackages)
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.DropOldest,
            }
        );

    readonly Channel<T> sendQueue = Channel.CreateBounded<T>(
        new BoundedChannelOptions(MaxQueuedPackages)
        {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropOldest,
        }
    );

    public event Func<T, CancellationToken, ValueTask> OnMessage = delegate
    {
        return ValueTask.CompletedTask;
    };

    public IPEndPoint Peer { get; } = peer;
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
        return socket;
    }

    async Task Produce(CancellationToken ct)
    {
        var buffer = GC.AllocateArray<byte>(
                length: MaxUdpSize,
                pinned: true
            )
            .AsMemory();

        while (!ct.IsCancellationRequested)
            try
            {
                var receivedSize = await socket
                    .ReceiveFromAsync(buffer, SocketFlags.None, peerAddress, ct);

                if (receivedSize is 0) continue;

                await channel.Writer.WriteAsync(buffer[..receivedSize], ct);
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
            await foreach (var pkg in channel.Reader.ReadAllAsync(ct))
            {
                var parsed = serializer.Deserialize(pkg.Span);
                await OnMessage.Invoke(parsed, ct);
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
            await foreach (var msg in sendQueue.Reader.ReadAllAsync(ct))
            {
                var bodySize = serializer.Serialize(msg, buffer.Span);
                var sentSize = await SendRaw(buffer[..bodySize], ct);
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

    public async ValueTask<int> SendRaw(ReadOnlyMemory<byte> payload, CancellationToken ct) =>
        await socket.SendToAsync(payload, SocketFlags.None, peerAddress, ct);

    public ValueTask Send(T payload, CancellationToken ct) =>
        sendQueue.Writer.WriteAsync(payload, ct);

    public void Dispose()
    {
        cancellation.Cancel();
        cancellation.Dispose();
        socket.Dispose();
        sendQueue.Writer.Complete();
    }
}