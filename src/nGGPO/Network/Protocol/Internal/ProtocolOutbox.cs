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

    readonly ushort magicNumber = MagicNumber.Generate();

    int packetsSent;
    int nextSendSeq;

    public long LastSendTime { get; private set; }
    readonly int sendLatency = options.NetworkDelay;

    public ValueTask SendMessage(ref ProtocolMessage msg, CancellationToken ct)
    {
        logger.LogMsg("send", msg);

        Interlocked.Increment(ref packetsSent);
        LastSendTime = TimeStamp.GetMilliseconds();

        msg.Header.Magic = magicNumber;
        Interlocked.Increment(ref nextSendSeq);
        msg.Header.SequenceNumber = (ushort)nextSendSeq;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(sendQueueCancellation.Token, ct);

        return sendQueue.Writer.WriteAsync(new()
        {
            QueueTime = TimeStamp.GetMilliseconds(),
            DestAddr = options.Peer.Address,
            Msg = msg,
        }, cts.Token);
    }

    public async Task Start(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(sendQueueCancellation.Token, ct);
        await foreach (var entry in sendQueue.Reader.ReadAllAsync(cts.Token).ConfigureAwait(false))
        {
            if (sendLatency > 0)
            {
                // should really come up with a gaussian distribution based on the configured
                // value, but this will do for now.
                var jitter = (sendLatency * 2 / 3) + (options.Random.Next() % sendLatency / 3);

                var delayDiff = TimeStamp.GetMilliseconds() - entry.QueueTime + jitter;
                if (delayDiff > 0)
                    await Task.Delay((int)delayDiff, cts.Token);
            }

            await udp.SendTo(entry.DestAddr, entry.Msg, sendQueueCancellation.Token).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        sendQueueCancellation.Cancel();
        sendQueueCancellation.Dispose();
        sendQueue.Writer.Complete();
    }
}
