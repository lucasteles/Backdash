using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol.Comm;
using Backdash.Options;

namespace Backdash.Network.Protocol;

sealed class ProtocolClientFactory(
    NetcodeOptions options,
    IPeerSocketFactory socketFactory,
    Logger logger,
    IDelayStrategy delayStrategy
)
{
    public PeerClient<ProtocolMessage> CreateClient(int port, IPeerObserver<ProtocolMessage> observer) =>
        new(
            socketFactory.Create(port, options),
            new ProtocolMessageSerializer(options.Protocol.SerializationEndianness),
            observer,
            logger,
            delayStrategy,
            options.Protocol.UdpPacketBufferSize,
            options.Protocol.MaxPackageQueue,
            options.Protocol.ReceiveSocketAddressSize
        )
        {
            NetworkLatency = options.Protocol.NetworkLatency,
        };
}
