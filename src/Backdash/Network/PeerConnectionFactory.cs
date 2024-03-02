using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Comm;
using Backdash.Serialization;
using Backdash.Sync;

namespace Backdash.Network;

sealed class PeerConnectionFactory
{
    readonly IRandomNumberGenerator random;
    readonly IDelayStrategy delayStrategy;
    readonly Logger logger;
    readonly IClock clock;
    readonly IBackgroundJobManager jobManager;
    readonly IProtocolNetworkEventHandler networkEventHandler;
    readonly IUdpClient<ProtocolMessage> udp;
    readonly ProtocolOptions options;
    readonly TimeSyncOptions timeSyncOptions;

    public PeerConnectionFactory(
        IClock clock,
        Random defaultRandom,
        Logger logger,
        IBackgroundJobManager jobManager,
        IProtocolNetworkEventHandler networkEventHandler,
        IUdpClient<ProtocolMessage> udp,
        ProtocolOptions options,
        TimeSyncOptions timeSyncOptions
    )
    {
        random = new DefaultRandomNumberGenerator(defaultRandom);
        delayStrategy = DelayStrategyFactory.Create(random, options.DelayStrategy);

        this.logger = logger;
        this.jobManager = jobManager;
        this.networkEventHandler = networkEventHandler;
        this.udp = udp;
        this.options = options;
        this.clock = clock;
        this.timeSyncOptions = timeSyncOptions;
    }

    public PeerConnection<TInput> Create<TInput>(
        ProtocolState state,
        IBinarySerializer<TInput> inputSerializer,
        IProtocolInputEventPublisher<TInput> inputEventQueue
    ) where TInput : struct
    {
        var timeSync = new TimeSync<TInput>(timeSyncOptions, logger);
        var outbox = new ProtocolOutbox(state, options, udp, delayStrategy, random, clock, logger);
        var syncManager = new ProtocolSynchronizer(logger, clock, random, state, options, outbox, networkEventHandler);
        var inbox = new ProtocolInbox<TInput>(options, inputSerializer, state, clock, syncManager, outbox,
            networkEventHandler, inputEventQueue, logger);
        var inputBuffer =
            new ProtocolInputBuffer<TInput>(options, inputSerializer, state, logger, timeSync, outbox, inbox);

        jobManager.Register(outbox, state.StoppingToken);

        PeerConnection<TInput> connection = new(
            options, state, logger, clock, timeSync, networkEventHandler,
            syncManager, inbox, outbox, inputBuffer
        );

        state.StoppingToken.Register(() => connection.Disconnect());
        return connection;
    }
}
