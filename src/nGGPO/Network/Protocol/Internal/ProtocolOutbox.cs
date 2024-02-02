using System.Net;
using System.Threading.Channels;
using nGGPO.Lifecycle;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol.Internal;

interface IProtocolOutbox : IMessageSender, IBackgroundJob, IDisposable;

sealed class ProtocolOutbox(
    ProtocolOptions options,
    IUdpClient<ProtocolMessage> udp,
    IDelayStrategy delayStrategy,
    IRandomNumberGenerator random,
    IClock clock,
    IProtocolLogger logger
) : IProtocolOutbox
{
    struct QueueEntry
    {
        public long QueueTime;
        public SocketAddress DestAddr;
        public ProtocolMessage Msg;
    }

    readonly Channel<QueueEntry> sendQueue =
        Channel.CreateBounded<QueueEntry>(
            new BoundedChannelOptions(64)
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            });

    readonly CancellationTokenSource sendQueueCancellation = new();

    readonly ushort magicNumber = random.MagicNumber();

    int packetsSent;
    int nextSendSeq;

    public string JobName { get; } = $"{nameof(ProtocolOutbox)} ({udp.Port})";

    public long LastSendTime { get; private set; }

    public ValueTask SendMessage(ref ProtocolMessage msg, CancellationToken ct)
    {
        logger.LogMsg("send", msg);

        Interlocked.Increment(ref packetsSent);
        LastSendTime = clock.GetMilliseconds();

        msg.Header.Magic = magicNumber;
        Interlocked.Increment(ref nextSendSeq);
        msg.Header.SequenceNumber = (ushort)nextSendSeq;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(sendQueueCancellation.Token, ct);

        return sendQueue.Writer.WriteAsync(new()
        {
            QueueTime = clock.GetMilliseconds(),
            DestAddr = options.Peer.Address,
            Msg = msg,
        }, cts.Token);
    }

    public async Task Start(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(sendQueueCancellation.Token, ct);
        var sendLatency = options.NetworkDelay;
        var reader = sendQueue.Reader;

        while (!ct.IsCancellationRequested)
        {
            await reader.WaitToReadAsync(ct).ConfigureAwait(false);

            while (reader.TryRead(out var entry))
            {
                if (sendLatency > 0)
                {
                    var jitter = delayStrategy.Jitter(sendLatency);
                    var delayDiff = clock.GetMilliseconds() - entry.QueueTime + jitter;
                    if (delayDiff > 0)
                        // LATER: allocations here
                        await Task.Delay((int)delayDiff, cts.Token).ConfigureAwait(false);
                }

                await udp.SendTo(entry.DestAddr, entry.Msg, sendQueueCancellation.Token).ConfigureAwait(false);
            }
        }
    }

    public void Dispose()
    {
        sendQueueCancellation.Cancel();
        sendQueueCancellation.Dispose();
        sendQueue.Writer.Complete();
    }
}
