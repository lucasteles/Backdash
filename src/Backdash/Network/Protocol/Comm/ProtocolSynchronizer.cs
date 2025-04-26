using System.Diagnostics;
using Backdash.Core;
using Backdash.Network.Messages;
using Backdash.Options;

namespace Backdash.Network.Protocol.Comm;

sealed class ProtocolSynchronizer(
    Logger logger,
    IRandomNumberGenerator random,
    ProtocolState state,
    ProtocolOptions options,
    IMessageSender sender,
    IProtocolNetworkEventHandler eventHandler
)
{
    bool active;
    int retryCounter;
    long lastRequest;

    public void RequestSync()
    {
        ProtocolMessage syncMsg = new();
        CreateRequestMessage(ref syncMsg);
        logger.Write(LogLevel.Debug, $"New Sync Request: {syncMsg.SyncRequest.RandomRequest} for {state.Player}");
        lastRequest = Stopwatch.GetTimestamp();
        sender.SendMessage(in syncMsg);
    }

    public void CreateRequestMessage(ref ProtocolMessage message)
    {
        lock (state.Sync.Locker)
        {
            state.Sync.CurrentRandom = random.SyncNumber();
            message.Header.Type = MessageType.SyncRequest;
            message.SyncRequest.RandomRequest = state.Sync.CurrentRandom;
            message.SyncRequest.Ping = Stopwatch.GetTimestamp();
        }
    }

    public void CreateReplyMessage(in SyncRequest request, ref ProtocolMessage replyMessage)
    {
        lock (state.Sync.Locker)
        {
            replyMessage.Header.Type = MessageType.SyncReply;
            replyMessage.SyncReply.RandomReply = request.RandomRequest;
            replyMessage.SyncReply.Pong = request.Ping;
        }
    }

    public void Synchronize()
    {
        state.Sync.RemainingRoundTrips = options.NumberOfSyncRoundtrips;
        state.CurrentStatus = ProtocolStatus.Syncing;
        retryCounter = 0;
        active = true;
        RequestSync();
        logger.Write(LogLevel.Information,
            $"Synchronize {state.Player} with {state.Sync.RemainingRoundTrips} roundtrips");
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

        var firstIteration = state.Sync.RemainingRoundTrips == options.NumberOfSyncRoundtrips;
        var interval = firstIteration ? options.SyncFirstRetryInterval : options.SyncRetryInterval;
        var elapsed = Stopwatch.GetElapsedTime(lastRequest);
        if (elapsed < interval)
            return;
        logger.Write(LogLevel.Information,
            $"No luck syncing {state.Player} after {(int)elapsed.TotalMilliseconds}ms. Re-queueing sync packet");
        RequestSync();
        retryCounter++;
    }
}
