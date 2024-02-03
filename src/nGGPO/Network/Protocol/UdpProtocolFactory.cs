using nGGPO.Core;
using nGGPO.Input;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol.Internal;

namespace nGGPO.Network.Protocol;

static class UdpProtocolFactory
{
    public static UdpProtocol CreateDefault(Random defaultRandom, ILogger logger,
        IBackgroundJobManager jobManager,
        IUdpObservableClient<ProtocolMessage> udp,
        Connections localConnections,
        ProtocolOptions options,
        TimeSyncOptions timeSyncOptions)
    {
        TimeSync timeSync = new(timeSyncOptions, logger);
        InputEncoder inputEncoder = new();
        ProtocolState state = new(localConnections, udp.Client.Port);
        ProtocolLogger udpLogger = new(logger);
        ProtocolEventDispatcher eventDispatcher = new(udpLogger);
        CryptographyRandomNumberGenerator random = new(defaultRandom);
        Clock clock = new();
        DelayStrategy delayStrategy = new(random);

        ProtocolOutbox outbox = new(options, udp.Client, delayStrategy, random, clock, udpLogger);
        ProtocolInbox inbox = new(
            options, state, random, clock, outbox, inputEncoder, eventDispatcher, udpLogger, logger
        );
        ProtocolInputProcessor inputProcessor = new(options, state, localConnections, logger,
            inputEncoder, timeSync, outbox, inbox);

        udp.Observers.Add(inbox);
        jobManager.Register(outbox);
        jobManager.Register(inputProcessor);

        return new(
            options,
            state,
            random,
            clock,
            timeSync,
            inbox,
            outbox,
            inputProcessor
        );
    }
}
