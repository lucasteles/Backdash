using Backdash.Core;
using Backdash.Network.Messages;

namespace Backdash.Network.Protocol.Comm;

interface IProtocolSynchronizer
{
    void CreateRequestMessage(out ProtocolMessage requestMessage);
    void CreateReplyMessage(in SyncRequest request, out ProtocolMessage replyMessage);
    void Synchronize();
    void Update();
}

sealed class ProtocolSynchronizer(
    Logger logger,
    IClock clock,
    IRandomNumberGenerator random,
    ProtocolState state,
    ProtocolOptions options,
    IMessageSender sender,
    IProtocolNetworkEventHandler eventHandler
) : IProtocolSynchronizer
{
    bool active;
    int retryCounter;
    long lastRequest;

    public void RequestSync()
    {
        CreateRequestMessage(out var syncMsg);
        logger.Write(LogLevel.Debug, $"New Sync Request: {syncMsg.SyncRequest.RandomRequest} for {state.Player}");
        lastRequest = clock.GetTimeStamp();
        sender.SendMessage(in syncMsg);
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

    public void Synchronize()
    {
        state.Sync.RemainingRoundtrips = options.NumberOfSyncPackets;
        state.CurrentStatus = ProtocolStatus.Syncing;
        retryCounter = 0;
        active = true;
        RequestSync();

        logger.Write(LogLevel.Information,
            $"Synchronize {state.Player} with {state.Sync.RemainingRoundtrips} roundtrips");
    }

    public void Update()
    {
        if (state.CurrentStatus is not ProtocolStatus.Syncing || !active)
            return;

        if (retryCounter >= options.MaxSyncRetries)
        {
            active = false;
            logger.Write(LogLevel.Warning,
                $"Fail to sync {state.Player} after {retryCounter} retries");
            eventHandler.OnNetworkEvent(ProtocolEvent.SyncFailure, state.Player);
            return;
        }

        var firstIteration = state.Sync.RemainingRoundtrips == options.NumberOfSyncPackets;
        var interval = firstIteration ? options.SyncFirstRetryInterval : options.SyncRetryInterval;
        var elapsed = clock.GetElapsedTime(lastRequest);
        if (elapsed < interval)
            return;

        logger.Write(LogLevel.Information,
            $"No luck syncing {state.Player} after {(int)elapsed.TotalMilliseconds}ms. Re-queueing sync packet");

        RequestSync();
        retryCounter++;
    }
}
