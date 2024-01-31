using nGGPO.Input;
using nGGPO.Lifecycle;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol.Internal;

namespace nGGPO.Network.Protocol;

static class UdpProtocolFactory
{
    public static UdpProtocol CreateDefault(
        ILogger logger,
        IBackgroundJobManager jobManager,
        IUdpObservableClient<ProtocolMessage> udp,
        Connections localConnections,
        ProtocolOptions options,
        TimeSyncOptions timeSyncOptions
    )
    {
        TimeSync timeSync = new(timeSyncOptions, logger);
        InputEncoder inputEncoder = new();
        ProtocolState state = new(localConnections, udp.Client.Port);
        ProtocolLogger udpLogger = new(logger);
        ProtocolEventDispatcher eventDispatcher = new(udpLogger);
        DelayStrategy delayStrategy = new(options.Random);

        ProtocolOutbox outbox = new(options, udp.Client, delayStrategy, udpLogger);
        ProtocolInbox inbox = new(options, state, outbox, inputEncoder, eventDispatcher, udpLogger, logger);
        ProtocolInputProcessor inputProcessor = new(options, state, localConnections, logger,
            inputEncoder, timeSync, outbox, inbox);

        udp.Observers.Add(inbox);
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
