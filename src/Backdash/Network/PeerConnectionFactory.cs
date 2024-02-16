using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Messaging;
using Backdash.Sync;

namespace Backdash.Network;

static class PeerConnectionFactory
{
    public static PeerConnection CreateDefault(
        ProtocolState state,
        Random defaultRandom,
        Logger logger,
        IBackgroundJobManager jobManager,
        IUdpClient<ProtocolMessage> udp,
        IProtocolEventQueue eventQueue,
        ProtocolOptions options,
        TimeSyncOptions timeSyncOptions
    )
    {
        TimeSync timeSync = new(timeSyncOptions, logger);
        CryptographyRandomNumberGenerator random = new(defaultRandom);
        Clock clock = new();
        DelayStrategy delayStrategy = new(random);
        ProtocolOutbox outbox = new(state, options, udp, delayStrategy, random, clock, logger);
        ProtocolSyncManager syncManager = new(logger, clock, random, jobManager, state, options, outbox);
        ProtocolInbox inbox = new(
            options, state, clock, syncManager, outbox, eventQueue, logger
        );
        ProtocolInputBuffer inputBuffer = new(options, state, logger, timeSync, outbox, inbox);
        jobManager.Register(outbox, state.StoppingToken);

        return new(
            options, state, logger, clock, timeSync, eventQueue,
            syncManager, inbox, outbox, inputBuffer
        );
    }
}
