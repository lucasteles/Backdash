global using IProtocolClient = Backdash.Network.Client.IPeerJobClient<Backdash.Network.Messages.ProtocolMessage>;
using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol.Comm;

namespace Backdash.Network.Protocol;

interface IProtocolClientFactory
{
    IProtocolClient CreateProtocolClient(int port, IPeerObserver<ProtocolMessage> observer);
}

sealed class ProtocolClientFactory(
    RollbackOptions options,
    IPeerSocketFactory socketFactory,
    IClock clock,
    Logger logger,
    IDelayStrategy delayStrategy
) : IProtocolClientFactory
{
    public IProtocolClient CreateProtocolClient(int port, IPeerObserver<ProtocolMessage> observer) =>
        new PeerClient<ProtocolMessage>(
            socketFactory.Create(port, options),
            new ProtocolMessageBinarySerializer(options.NetworkEndianness),
            observer,
            logger,
            clock,
            delayStrategy,
            options.Protocol.UdpPacketBufferSize,
            options.Protocol.MaxPackageQueue
        )
        {
            NetworkLatency = options.Protocol.NetworkLatency,
        };
}
