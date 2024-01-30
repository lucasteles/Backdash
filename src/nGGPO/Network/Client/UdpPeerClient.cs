using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using nGGPO.Serialization;
using nGGPO.Utils;

namespace nGGPO.Network.Client;

sealed class UdpClient<T>(
    int port,
    IUdpObserver<T> observer,
    IBinarySerializer<T> serializer
) : IDisposable
    where T : struct
{
    public bool LogsEnabled = true;
    readonly Socket socket = CreateSocket(port);
    CancellationTokenSource? cancellation;
    public int Port => port;
    public SocketAddress Address { get; } = new IPEndPoint(IPAddress.Loopback, port).Serialize();
    public uint TotalBytesSent { get; private set; }

    readonly Channel<(SocketAddress, T)> sendQueue =
        Channel.CreateUnbounded<(SocketAddress, T)>(
            new()
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = true,
            }
        );

    public async Task StartPumping(CancellationToken cancellationToken = default)
    {
        if (cancellation is not null)
            return;

        cancellation = new();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellation.Token);

        await Task.WhenAll(
            ReadLoop(cts.Token),
            SendLoop(cts.Token)
        ).ConfigureAwait(false);
    }

    public async Task StopPumping()
    {
        if (cancellation is null)
            return;

        await cancellation.CancelAsync().ConfigureAwait(false);
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
                    Tracer.Warn(ex, "Socket error");

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


        try
        {
            await foreach (var (peerAddress, nextMsg) in sendQueue.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                var msg = nextMsg;
                var bodySize = serializer.Serialize(ref msg, sendBuffer.Span);
                var sentSize = await SendBytes(peerAddress, sendBuffer[..bodySize], ct).ConfigureAwait(false);
                Tracer.Assert(sentSize == bodySize);
                if (ct.IsCancellationRequested) break;
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
        TotalBytesSent += (uint)payload.Length;
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
