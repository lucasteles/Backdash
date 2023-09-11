using Backdash.Core;
using Backdash.Input;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Events;
using Backdash.Network.Protocol.Messaging;

namespace Backdash.Network;

static class PeerConnectionFactory
{
    public static PeerConnection CreateDefault(Random defaultRandom, ILogger logger,
        IBackgroundJobManager jobManager,
        IUdpClient<ProtocolMessage> udp,
        ConnectionStatuses localConnections,
        ProtocolOptions options,
        TimeSyncOptions timeSyncOptions)
    {
        TimeSync timeSync = new(timeSyncOptions, logger);
        InputEncoder inputEncoder = new();
        ProtocolState state = new(localConnections, udp.Port);
        ProtocolLogger udpLogger = new(logger);
        ProtocolEventDispatcher eventDispatcher = new(udpLogger);
        CryptographyRandomNumberGenerator random = new(defaultRandom);
        Clock clock = new();
        DelayStrategy delayStrategy = new(random);

        ProtocolOutbox outbox = new(options, udp, delayStrategy, random, clock, udpLogger);
        ProtocolInbox inbox = new(
            options, state, random, clock, outbox, inputEncoder, eventDispatcher, udpLogger, logger
        );
        ProtocolInputProcessor inputProcessor = new(options, state, localConnections, logger,
            inputEncoder, timeSync, outbox, inbox);

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
