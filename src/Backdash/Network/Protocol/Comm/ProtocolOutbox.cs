using System.Net;
using System.Threading.Channels;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Client;
using Backdash.Network.Messages;

namespace Backdash.Network.Protocol.Comm;

interface IProtocolOutbox : IMessageSender, IBackgroundJob, IDisposable;

sealed class ProtocolOutbox(
    ProtocolState state,
    ProtocolOptions options,
    IUdpClient<ProtocolMessage> udp,
    IDelayStrategy delayStrategy,
    IRandomNumberGenerator random,
    IClock clock,
    Logger logger
) : IProtocolOutbox
{
    struct QueueEntry
    {
        public long QueueTime;
        public SocketAddress Recipient;
        public ProtocolMessage Body;
    }

    readonly Channel<QueueEntry> sendQueue =
        Channel.CreateBounded<QueueEntry>(
            new BoundedChannelOptions(options.MaxPackageQueue)
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            });

    readonly ushort magicNumber = random.MagicNumber();

    int nextSendSeq;

    public string JobName { get; } = $"{nameof(ProtocolOutbox)} {state.Player} {udp.Port}";

    QueueEntry CreateNextEntry(in ProtocolMessage msg) =>
        new()
        {
            QueueTime = clock.GetTimeStamp(),
            Recipient = state.PeerAddress.Address,
            Body = msg,
        };

    public ValueTask SendMessageAsync(in ProtocolMessage msg, CancellationToken ct)
    {
        var nextEntry = CreateNextEntry(in msg);
        return sendQueue.Writer.WriteAsync(nextEntry, ct);
    }

    public bool SendMessage(in ProtocolMessage msg)
    {
        var nextEntry = CreateNextEntry(in msg);
        return sendQueue.Writer.TryWrite(nextEntry);
    }

    public async Task Start(CancellationToken ct)
    {
        var sendLatency = options.NetworkDelay;
        var reader = sendQueue.Reader;

        var buffer = Mem.CreatePinnedMemory(options.UdpPacketBufferSize);

        while (!ct.IsCancellationRequested)
        {
            await reader.WaitToReadAsync(ct).ConfigureAwait(false);

            while (reader.TryRead(out var entry))
            {
                var message = entry.Body;
                message.Header.Magic = magicNumber;
                message.Header.SequenceNumber = (ushort)nextSendSeq;
                nextSendSeq++;

                logger.Write(LogLevel.Trace, $"send {message} on {state.Player}");

                if (sendLatency > TimeSpan.Zero)
                {
                    var jitter = delayStrategy.Jitter(sendLatency);
                    SpinWait sw = new();
                    while (clock.GetElapsedTime(entry.QueueTime) <= jitter)
                    {
                        sw.SpinOnce();
                        // LATER: allocations here with Task.Delay
                        // await Task.Delay(delayDiff, ct).ConfigureAwait(false)
                    }
                }

                var bytesSent = await udp
                    .SendTo(entry.Recipient, message, buffer, ct)
                    .ConfigureAwait(false);

                state.Stats.Send.LastTime = clock.GetTimeStamp();
                state.Stats.Send.TotalBytes += (ByteSize)bytesSent;
                state.Stats.Send.TotalPackets++;
            }
        }
    }

    public void Dispose() => sendQueue.Writer.TryComplete();
}
