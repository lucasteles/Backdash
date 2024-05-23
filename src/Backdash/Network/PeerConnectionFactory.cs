using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Comm;
using Backdash.Serialization;
using Backdash.Synchronizing;

namespace Backdash.Network;

sealed class PeerConnectionFactory(
    IProtocolNetworkEventHandler networkEventHandler,
    IClock clock,
    IRandomNumberGenerator random,
    IDelayStrategy delayStrategy,
    Logger logger,
    IBackgroundJobManager jobManager,
    IPeerClient<ProtocolMessage> peer,
    ProtocolOptions options,
    TimeSyncOptions timeSyncOptions
)
{
    public PeerConnection<TInput> Create<TInput>(
        ProtocolState state,
        IBinarySerializer<TInput> inputSerializer,
        IProtocolInputEventPublisher<TInput> inputEventQueue
    ) where TInput : unmanaged
    {
        var timeSync = new TimeSync<TInput>(timeSyncOptions, logger);
        var outbox = new ProtocolOutbox(state, options, peer, delayStrategy, clock, logger);
        var syncManager = new ProtocolSynchronizer(logger, clock, random, state, options, outbox, networkEventHandler);
        var inbox = new ProtocolInbox<TInput>(options, inputSerializer, state, clock, syncManager, outbox,
            networkEventHandler, inputEventQueue, logger);
        var inputBuffer = new ProtocolInputBuffer<TInput>(
            options, inputSerializer, state, logger, timeSync, outbox, inbox);

        jobManager.Register(outbox, state.StoppingToken);

        PeerConnection<TInput> connection = new(
            options, state, logger, clock, timeSync, networkEventHandler,
            syncManager, inbox, outbox, inputBuffer
        );
        state.StoppingToken.Register(() => connection.Disconnect());

        return connection;
    }
}
