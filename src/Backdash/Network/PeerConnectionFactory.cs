using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Messaging;
using Backdash.Sync;

namespace Backdash.Network;

sealed class PeerConnectionFactory
{
    readonly IClock clock = new Clock();
    readonly IRandomNumberGenerator random;
    readonly IDelayStrategy delayStrategy;
    readonly Logger logger;
    readonly IBackgroundJobManager jobManager;
    readonly IUdpClient<ProtocolMessage> udp;
    readonly IProtocolEventQueue eventQueue;
    readonly ProtocolOptions options;
    readonly TimeSyncOptions timeSyncOptions;

    public PeerConnectionFactory(
        Random defaultRandom,
        Logger logger,
        IBackgroundJobManager jobManager,
        IUdpClient<ProtocolMessage> udp,
        IProtocolEventQueue eventQueue,
        ProtocolOptions options,
        TimeSyncOptions timeSyncOptions
    )
    {
        random = new DefaultRandomNumberGenerator(defaultRandom);
        delayStrategy = DelayStrategyFactory.Create(random, options.DelayStrategy);

        this.logger = logger;
        this.jobManager = jobManager;
        this.udp = udp;
        this.eventQueue = eventQueue;
        this.options = options;
        this.timeSyncOptions = timeSyncOptions;
    }

    public PeerConnection Create(ProtocolState state)
    {
        var timeSync = new TimeSync(timeSyncOptions, logger);
        var outbox = new ProtocolOutbox(state, options, udp, delayStrategy, random, clock, logger);
        var syncManager = new ProtocolSyncManager(logger, clock, random, jobManager, state, options, outbox);
        var inbox = new ProtocolInbox(options, state, clock, syncManager, outbox, eventQueue, logger);
        var inputBuffer = new ProtocolInputBuffer(options, state, logger, timeSync, outbox, inbox);

        jobManager.Register(outbox, state.StoppingToken);

        return new(
            options, state, logger, clock, timeSync, eventQueue,
            syncManager, inbox, outbox, inputBuffer
        );
    }
}
