using nGGPO.Input;
using nGGPO.Lifecycle;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol.Internal;

namespace nGGPO.Network.Protocol;

static class UdpProtocolFactory
{
    public static UdpProtocol CreateDefault(
        BackgroundJobManager jobManager,
        UdpObserverGroup<ProtocolMessage> peerObservers,
        ProtocolOptions options,
        IUdpClient<ProtocolMessage> udp,
        Connections localConnections
    )
    {
        TimeSync timeSync = new();
        InputEncoder inputEncoder = new();
        ProtocolState state = new(localConnections);
        ProtocolLogger logger = new();
        ProtocolEventDispatcher eventDispatcher = new(logger);
        DelayStrategy delayStrategy = new(options.Random);

        ProtocolOutbox outbox = new(options, udp, delayStrategy, logger);
        ProtocolInbox inbox = new(options, state, outbox, inputEncoder, eventDispatcher, logger);
        ProtocolInputProcessor inputProcessor = new(options, state, localConnections,
            inputEncoder, timeSync, outbox, inbox);

        peerObservers.Add(inbox);
        jobManager.Register(outbox);
        jobManager.Register(inputProcessor);

        return new(
            options,
            state,
            timeSync,
            inbox,
            outbox,
            inputProcessor
        );
    }
}
