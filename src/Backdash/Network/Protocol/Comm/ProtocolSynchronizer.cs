using Backdash.Core;
using Backdash.Network.Messages;

namespace Backdash.Network.Protocol.Comm;

interface IProtocolSynchronizer
{
    void CreateRequestMessage(out ProtocolMessage requestMessage);
    void CreateReplyMessage(in SyncRequest request, out ProtocolMessage replyMessage);
    void Synchronize();
}

sealed class ProtocolSynchronizer(
    Logger logger,
    IClock clock,
    IRandomNumberGenerator random,
    IBackgroundJobManager jobManager,
    ProtocolState state,
    ProtocolOptions options,
    IMessageSender sender,
    IProtocolNetworkEventHandler eventHandler
) : IBackgroundJob, IProtocolSynchronizer
{
    public string JobName => $"Synchronizer {state.Player}";

    public async ValueTask RequestSync(CancellationToken ct)
    {
        CreateRequestMessage(out var syncMsg);
        logger.Write(LogLevel.Debug, $"New Sync Request: {syncMsg.SyncRequest.RandomRequest} for {state.Player}");
        await sender.SendMessageAsync(in syncMsg, ct);
    }

    public void CreateRequestMessage(out ProtocolMessage requestMessage)
    {
        lock (state.Sync.Locker)
        {
            state.Sync.CurrentRandom = random.SyncNumber();
            requestMessage = new(MessageType.SyncRequest)
            {
                SyncRequest = new()
                {
                    RandomRequest = state.Sync.CurrentRandom,
                    Ping = clock.GetTimeStamp(),
                },
            };
        }
    }

    public void CreateReplyMessage(in SyncRequest request, out ProtocolMessage replyMessage)
    {
        lock (state.Sync.Locker)
        {
            replyMessage = new(MessageType.SyncReply)
            {
                SyncReply = new()
                {
                    RandomReply = request.RandomRequest,
                    Pong = request.Ping,
                },
            };
        }
    }

    public async Task Start(CancellationToken ct)
    {
        logger.Write(LogLevel.Debug, $"Sync job: started for {state.Player}");

        if (state.CurrentStatus is ProtocolStatus.Running)
        {
            logger.Write(LogLevel.Trace, $"Sync job: already running for {state.Player}. skipping sync");
            return;
        }

        var retryCounter = 0;

        do
        {
            var time = clock.GetTimeStamp();
            await RequestSync(ct);

            var firstIteration = state.Sync.RemainingRoundtrips == options.NumberOfSyncPackets;

            var interval = (firstIteration ? options.SyncFirstRetryInterval : options.SyncRetryInterval)
                           - clock.GetElapsedTime(time);

            if (interval > TimeSpan.Zero)
                await Task.Delay(interval, ct).ConfigureAwait(false);
            else
                await Task.Delay(Default.SyncFirstRetryInterval, ct);

            if (state.CurrentStatus is ProtocolStatus.Syncing)
            {
                if (retryCounter >= options.MaxSyncRetries)
                {
                    logger.Write(LogLevel.Warning,
                        $"Fail to sync {state.Player} after {retryCounter} retries");
                    eventHandler.OnNetworkEvent(ProtocolEvent.SyncFailure, state.Player);
                    return;
                }

                logger.Write(LogLevel.Information,
                    $"No luck syncing {state.Player} after {(int)clock.GetElapsedTime(time).TotalMilliseconds}ms. Re-queueing sync packet");

                retryCounter++;
            }
        } while (!ct.IsCancellationRequested && state.CurrentStatus is ProtocolStatus.Syncing);

        logger.Write(LogLevel.Debug, $"Sync job: complete with status {state.CurrentStatus}");
    }

    public void Synchronize()
    {
        state.Sync.RemainingRoundtrips = options.NumberOfSyncPackets;
        state.CurrentStatus = ProtocolStatus.Syncing;
        jobManager.Register(this, state.StoppingToken);
        logger.Write(LogLevel.Information,
            $"Synchronize {state.Player} with {state.Sync.RemainingRoundtrips} roundtrips");
    }
}
