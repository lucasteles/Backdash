using Backdash.Core;
using Backdash.Network.Messages;

namespace Backdash.Network.Protocol.Messaging;

interface IProtocolSyncManager
{
    ValueTask RequestSync(CancellationToken ct);
    void CreateRequestMessage(out ProtocolMessage requestMessage);
    void CreateReplyMessage(in SyncRequest request, out ProtocolMessage replyMessage);
    void BeginSynchronization();
}

sealed class ProtocolSyncManager(
    Logger logger,
    IClock clock,
    IRandomNumberGenerator random,
    IBackgroundJobManager jobManager,
    ProtocolState state,
    ProtocolOptions options,
    IMessageSender sender
) : IBackgroundJob, IProtocolSyncManager
{
    public string JobName => $"Sync Timer {state.Player}";

    long lastMessageTimestamp;

    public async ValueTask RequestSync(CancellationToken ct)
    {
        if (!IsReadyForSend())
            return;

        CreateRequestMessage(out var syncMsg);
        logger.Write(LogLevel.Information, $"Sync Request Start: {syncMsg}");
        await sender.SendMessageAsync(in syncMsg, ct);
    }

    bool IsReadyForSend() => clock.GetElapsedTime(lastMessageTimestamp) > options.SyncRetryInterval;

    // TODO: also calculate ping on reply
    public void CreateRequestMessage(out ProtocolMessage requestMessage)
    {
        lock (state.Sync.Locker)
        {
            lastMessageTimestamp = clock.GetTimeStamp();
            state.Sync.CurrentRandom = random.SyncNumber();
            requestMessage = new(MsgType.SyncRequest)
            {
                SyncRequest = new()
                {
                    RandomRequest = state.Sync.CurrentRandom,
                },
            };
        }
    }

    public void CreateReplyMessage(in SyncRequest request, out ProtocolMessage replyMessage)
    {
        lock (state.Sync.Locker)
        {
            lastMessageTimestamp = clock.GetTimeStamp();
            replyMessage = new(MsgType.SyncReply)
            {
                SyncReply = new()
                {
                    RandomReply = request.RandomRequest,
                },
            };
        }
    }

    public async Task Start(CancellationToken ct)
    {
        logger.Write(LogLevel.Debug, "Sync job: started");

        if (state.CurrentStatus is ProtocolStatus.Running)
        {
            logger.Write(LogLevel.Trace, "Sync job: already running... skipping sync.");
            return;
        }

        state.CurrentStatus = ProtocolStatus.Syncing;
        state.Sync.RemainingRoundtrips = (uint)options.NumberOfSyncPackets;
        await RequestSync(ct);

        await Task.Delay(options.SyncFirstRetryInterval, ct);
        using PeriodicTimer timer = new(options.SyncRetryInterval);

        do
        {
            lock (state.Sync.Locker)
            {
                if (state.CurrentStatus is not ProtocolStatus.Syncing || ct.IsCancellationRequested)
                    break;

                if (!IsReadyForSend())
                    continue;

                logger.Write(LogLevel.Debug,
                    $"No luck syncing after {(int)clock.GetElapsedTime(lastMessageTimestamp).TotalMilliseconds}ms... Re-queueing sync packet");
            }

            await RequestSync(ct);
        } while (await timer.WaitForNextTickAsync(ct));

        logger.Write(LogLevel.Debug, $"Sync job: complete with status {state.CurrentStatus}");
    }

    public void BeginSynchronization() => jobManager.Register(this, state.StoppingToken);
}
