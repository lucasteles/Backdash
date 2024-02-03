using System.Net;
using System.Threading.Channels;
using nGGPO.Core;
using nGGPO.Data;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;

namespace nGGPO.Network.Protocol.Messaging;

interface IProtocolOutbox : IMessageSender, IBackgroundJob, IDisposable
{
    public ByteSize BytesSent { get; }
}

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
    public ByteSize BytesSent { get; private set; }

    QueueEntry CreateNextEntry(ref ProtocolMessage msg)
    {
        packetsSent++;
        LastSendTime = clock.GetMilliseconds();

        msg.Header.Magic = magicNumber;
        nextSendSeq++;
        msg.Header.SequenceNumber = (ushort)nextSendSeq;

        return new()
        {
            QueueTime = clock.GetMilliseconds(),
            DestAddr = options.Peer.Address,
            Msg = msg,
        };
    }

    public ValueTask SendMessage(ref ProtocolMessage msg, CancellationToken ct)
    {
        logger.LogMsg("send", msg);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(sendQueueCancellation.Token, ct);
        var nextEntry = CreateNextEntry(ref msg);
        return sendQueue.Writer.WriteAsync(nextEntry, cts.Token);
    }

    public bool TrySendMessage(ref ProtocolMessage msg)
    {
        logger.LogMsg("send", msg);
        var nextEntry = CreateNextEntry(ref msg);
        return sendQueue.Writer.TryWrite(nextEntry);
    }

    public async Task Start(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(sendQueueCancellation.Token, ct);
        var sendLatency = options.NetworkDelay;
        var reader = sendQueue.Reader;

        var buffer = Mem.CreatePinnedBuffer(options.UdpPacketBufferSize);

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

                var bytesSent = await udp
                    .SendTo(entry.DestAddr, entry.Msg, buffer, sendQueueCancellation.Token)
                    .ConfigureAwait(false);

                BytesSent += (ByteSize)bytesSent;
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
