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
        var timeSync = new TimeSync(timeSyncOptions, logger);
        var random = new DefaultRandomNumberGenerator(defaultRandom);
        var clock = new Clock();
        var delayStrategy = DelayStrategyFactory.Create(random, options.DelayStrategy);
        var outbox = new ProtocolOutbox(state, options, udp, delayStrategy, random, clock, logger);
        var syncManager = new ProtocolSyncManager(logger, clock, random, jobManager, state, options, outbox);
        var inbox = new ProtocolInbox(
            options, state, clock, syncManager, outbox, eventQueue, logger
        );
        var inputBuffer = new ProtocolInputBuffer(options, state, logger, timeSync, outbox, inbox);

        jobManager.Register(outbox, state.StoppingToken);

        return new(
            options, state, logger, clock, timeSync, eventQueue,
            syncManager, inbox, outbox, inputBuffer
        );
    }
}
