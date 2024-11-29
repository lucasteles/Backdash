using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Backdash.Core;
using Backdash.Serialization;

namespace Backdash.Network.Client;

/// <summary>
/// Client for peer communication
/// </summary>
public interface IPeerClient<T> : IDisposable where T : struct
{
    /// <summary>
    /// Send Message to peer
    /// </summary>
    ValueTask SendTo(SocketAddress peerAddress, in T payload, IMessageHandler<T>? callback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Try To Send Message to peer
    /// </summary>
    bool TrySendTo(SocketAddress peerAddress, in T payload, IMessageHandler<T>? callback = null);


    /// <summary>
    /// Start receiving messages
    /// </summary>
    Task ProcessMessages(CancellationToken cancellationToken);
}

/// <summary>
/// Message sent handler
/// </summary>
public interface IMessageHandler<T> where T : struct
{
    /// <summary>
    /// Handles sent message
    /// </summary>
    void AfterSendMessage(int bytesSent);

    /// <summary>
    /// Prepare message to be sent
    /// </summary>
    void BeforeSendMessage(ref T message);
}

interface IPeerJobClient<T> : IBackgroundJob, IPeerClient<T> where T : struct;

sealed class PeerClient<T> : IPeerJobClient<T> where T : struct
{
    readonly IPeerSocket socket;
    readonly IPeerObserver<T> observer;
    readonly IBinarySerializer<T> serializer;
    readonly Logger logger;
    readonly IClock clock;
    readonly IDelayStrategy? delayStrategy;
    readonly int maxPacketSize;
    readonly Channel<QueueEntry> sendQueue;
    CancellationTokenSource? cancellation;
    public string JobName { get; }

    public TimeSpan NetworkLatency = TimeSpan.Zero;

    struct QueueEntry(T body, SocketAddress recipient, long queuedAt, IMessageHandler<T>? callback)
    {
        public T Body = body;
        public readonly SocketAddress Recipient = recipient;
        public readonly long QueuedAt = queuedAt;
        public readonly IMessageHandler<T>? Callback = callback;
    }

    public PeerClient(
        IPeerSocket socket,
        IBinarySerializer<T> serializer,
        IPeerObserver<T> observer,
        Logger logger,
        IClock clock,
        IDelayStrategy? delayStrategy = null,
        int maxPacketSize = Max.UdpPacketSize,
        int maxPackageQueue = Default.MaxPackageQueue
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
        this.clock = clock;
        this.delayStrategy = delayStrategy;
        this.maxPacketSize = maxPacketSize;

        sendQueue = Channel.CreateBounded<QueueEntry>(
            new BoundedChannelOptions(maxPackageQueue)
            {
                SingleWriter = false,
                SingleReader = true,
                AllowSynchronousContinuations = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            });

        JobName = $"{nameof(UdpClient)} ({socket.Port})";
    }

    public Task Start(CancellationToken cancellationToken) => ProcessMessages(cancellationToken);

    public async Task ProcessMessages(CancellationToken cancellationToken)
    {
        if (cancellation is not null)
            return;

        cancellation = new();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellation.Token);
        var token = cts.Token;

        await Task.WhenAll(StartReceiving(token), StartSending(token)).ConfigureAwait(false);
    }

    async Task StartSending(CancellationToken cancellationToken)
    {
        var buffer = Mem.AllocatePinnedMemory(maxPacketSize);
        var reader = sendQueue.Reader;
        cancellationToken.Register(() => sendQueue.Writer.TryComplete());

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await reader.WaitToReadAsync(default).ConfigureAwait(false)
                    || cancellationToken.IsCancellationRequested)
                    break;

                while (reader.TryRead(out var entry))
                {
                    if (NetworkLatency > TimeSpan.Zero && delayStrategy is not null)
                    {
                        var jitter = delayStrategy.Jitter(NetworkLatency);
                        SpinWait sw = new();
                        while (clock.GetElapsedTime(entry.QueuedAt) <= jitter)
                        {
                            sw.SpinOnce();
                            // LATER: allocations here with Task.Delay
                            // await Task.Delay(delayDiff, ct).ConfigureAwait(false)
                        }
                    }

                    entry.Callback?.BeforeSendMessage(ref entry.Body);

                    var bodySize = serializer.Serialize(in entry.Body, buffer.Span);
                    var sentSize = await socket.SendToAsync(buffer[..bodySize], entry.Recipient, cancellationToken)
                        .ConfigureAwait(false);

                    Trace.Assert(sentSize == bodySize);

                    entry.Callback?.AfterSendMessage(sentSize);
                }
            }
            catch (Exception ex)
                when (ex is TaskCanceledException or OperationCanceledException or ChannelClosedException)
            {
                break;
            }
            catch (SocketException ex)
            {
                if (logger.EnabledLevel is not LogLevel.None)
                    logger.Write(LogLevel.Error, $"Socket send error: {ex}");
                break;
            }
            catch (Exception ex)
            {
                if (logger.EnabledLevel is not LogLevel.None)
                    logger.Write(LogLevel.Error, $"Socket send error: {ex}");
                break;
            }
        }
    }

    async Task StartReceiving(CancellationToken cancellationToken)
    {
        var buffer = Mem.AllocatePinnedArray(maxPacketSize);
        SocketAddress address = new(socket.AddressFamily);
        T msg = default;
        while (!cancellationToken.IsCancellationRequested)
        {
            int receivedSize;
            try
            {
                receivedSize = await socket
                    .ReceiveFromAsync(buffer, address, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex)
            {
                if (logger.EnabledLevel is not LogLevel.None)
                    logger.Write(LogLevel.Error, $"Socket rcv error: {ex}");
                break;
            }
            catch (Exception ex)
            {
                if (logger.EnabledLevel is not LogLevel.None)
                    logger.Write(LogLevel.Error, $"Socket rcv error: {ex}");
                break;
            }

            if (receivedSize is 0)
                continue;

            try
            {
                serializer.Deserialize(buffer.AsSpan(..receivedSize), ref msg);
                observer.OnPeerMessage(in msg, address, receivedSize);
            }
            catch (NetcodeDeserializationException ex)
            {
                logger.Write(LogLevel.Warning, $"UDP Message error: {ex}");
            }
        }

        // ReSharper disable once RedundantAssignment
#pragma warning disable S1854
        buffer = null;
#pragma warning restore S1854
    }

    public ValueTask SendTo(SocketAddress peerAddress, in T payload,
        IMessageHandler<T>? callback = null, CancellationToken cancellationToken = default) =>
        sendQueue.Writer.WriteAsync(new(payload, peerAddress, clock.GetTimeStamp(), callback), cancellationToken);

    public bool TrySendTo(SocketAddress peerAddress, in T payload, IMessageHandler<T>? callback = null) =>
        sendQueue.Writer.TryWrite(new(payload, peerAddress, clock.GetTimeStamp(), callback));

    public void Dispose()
    {
        sendQueue.Writer.TryComplete();
        cancellation?.Cancel();
        cancellation?.Dispose();
        socket.Close();
        socket.Dispose();
    }
}
