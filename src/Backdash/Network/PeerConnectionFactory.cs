using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Messaging;
using Backdash.Serialization;
using Backdash.Sync;

namespace Backdash.Network;

sealed class PeerConnectionFactory<TInput> where TInput : struct
{
    readonly IRandomNumberGenerator random;
    readonly IDelayStrategy delayStrategy;
    readonly IBinarySerializer<TInput> inputSerializer;
    readonly Logger logger;
    readonly IClock clock;
    readonly IBackgroundJobManager jobManager;
    readonly IUdpClient<ProtocolMessage> udp;
    readonly IProtocolEventQueue<TInput> eventQueue;
    readonly ProtocolOptions options;
    readonly TimeSyncOptions timeSyncOptions;

    public PeerConnectionFactory(
        IBinarySerializer<TInput> inputSerializer,
        IClock clock,
        Random defaultRandom,
        Logger logger,
        IBackgroundJobManager jobManager,
        IUdpClient<ProtocolMessage> udp,
        IProtocolEventQueue<TInput> eventQueue,
        ProtocolOptions options,
        TimeSyncOptions timeSyncOptions)
    {
        random = new DefaultRandomNumberGenerator(defaultRandom);
        delayStrategy = DelayStrategyFactory.Create(random, options.DelayStrategy);

        this.inputSerializer = inputSerializer;
        this.logger = logger;
        this.jobManager = jobManager;
        this.udp = udp;
        this.eventQueue = eventQueue;
        this.options = options;
        this.clock = clock;
        this.timeSyncOptions = timeSyncOptions;
    }

    public PeerConnection<TInput> Create(ProtocolState state)
    {
        var timeSync = new TimeSync<TInput>(timeSyncOptions, logger);
        var outbox = new ProtocolOutbox(state, options, udp, delayStrategy, random, clock, logger);
        var syncManager = new ProtocolSynchronizer(logger, clock, random, jobManager, state, options, outbox);
        var inbox = new ProtocolInbox<TInput>(options, inputSerializer, state, clock, syncManager, outbox, eventQueue,
            logger);
        var inputBuffer =
            new ProtocolInputBuffer<TInput>(options, inputSerializer, state, logger, timeSync, outbox, inbox);

        jobManager.Register(outbox, state.StoppingToken);

        return new(
            options, state, logger, clock, timeSync, eventQueue,
            syncManager, inbox, outbox, inputBuffer
        );
    }
}
