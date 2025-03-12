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
    NetcodeOptions options,
    IPeerSocketFactory socketFactory,
    Logger logger,
    IDelayStrategy delayStrategy
) : IProtocolClientFactory
{
    public IProtocolClient CreateProtocolClient(int port, IPeerObserver<ProtocolMessage> observer) =>
        new PeerClient<ProtocolMessage>(
            socketFactory.Create(port, options),
            new ProtocolMessageSerializer(options.Protocol.SerializationEndianness),
            observer,
            logger,
            delayStrategy,
            options.Protocol.UdpPacketBufferSize,
            options.Protocol.MaxPackageQueue
        )
        {
            NetworkLatency = options.Protocol.NetworkLatency,
        };
}
