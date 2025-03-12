using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Comm;
using Backdash.Options;
using Backdash.Serialization;
using Backdash.Synchronizing;
using Backdash.Synchronizing.State;

namespace Backdash.Network;

sealed class PeerConnectionFactory(
    IProtocolNetworkEventHandler networkEventHandler,
    IRandomNumberGenerator random,
    Logger logger,
    IPeerClient<ProtocolMessage> peer,
    ProtocolOptions options,
    TimeSyncOptions timeSyncOptions,
    IStateStore stateStore
)
{
    public PeerConnection<TInput> Create<TInput>(
        ProtocolState state,
        IBinarySerializer<TInput> inputSerializer,
        IProtocolInputEventPublisher<TInput> inputEventQueue,
        EqualityComparer<TInput>? inputComparer
    ) where TInput : unmanaged
    {
        var timeSync = new TimeSync<TInput>(timeSyncOptions, logger, inputComparer);
        var outbox = new ProtocolOutbox(state, peer, logger);
        var syncManager = new ProtocolSynchronizer(logger, random, state, options, outbox, networkEventHandler);
        var inbox = new ProtocolInbox<TInput>(options, inputSerializer, state, syncManager, outbox,
            networkEventHandler, inputEventQueue, stateStore, logger);
        var inputBuffer =
            new ProtocolInputBuffer<TInput>(options, inputSerializer, state, logger, timeSync, outbox, inbox);

        PeerConnection<TInput> connection = new(
            options, state, logger, timeSync, networkEventHandler,
            syncManager, inbox, outbox, inputBuffer, stateStore
        );

        state.StoppingToken.Register(() => connection.Disconnect());

        return connection;
    }
}
