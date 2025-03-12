global using IProtocolPeerClient = Backdash.Network.Client.IPeerJobClient<Backdash.Network.Messages.ProtocolMessage>;
using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol.Comm;
using Backdash.Options;

namespace Backdash.Network.Protocol;

interface IProtocolClientFactory
{
    IProtocolPeerClient CreateProtocolClient(int port, IPeerObserver<ProtocolMessage> observer);
}

sealed class ProtocolClientFactory(
    NetcodeOptions options,
    IPeerSocketFactory socketFactory,
    Logger logger,
    IDelayStrategy delayStrategy
) : IProtocolClientFactory
{
    public IProtocolPeerClient CreateProtocolClient(int port, IPeerObserver<ProtocolMessage> observer) =>
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
