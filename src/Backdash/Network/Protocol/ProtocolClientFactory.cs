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
    Logger logger
) : IProtocolClientFactory
{
    public IProtocolClient CreateProtocolClient(int port, IPeerObserver<ProtocolMessage> observer)
    {
        UdpSocket socket = new UdpSocket(port, options.UseIPv6);

        PeerClient<ProtocolMessage> peerClient = new(
            socket,
            new ProtocolMessageBinarySerializer(options.NetworkEndianness),
            observer,
            logger,
            options.Protocol.UdpPacketBufferSize
        );

        return peerClient;
    }
}
