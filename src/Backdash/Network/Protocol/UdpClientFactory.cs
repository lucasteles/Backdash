using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol.Messaging;

namespace Backdash.Network.Protocol;

interface IUdpClientFactory
{
    IUdpClient<ProtocolMessage> CreateClient(
        int port,
        bool enableEndianness,
        int maxPacketSize,
        IUdpObserver<ProtocolMessage> observer,
        ILogger logger
    );
}

sealed class UdpClientFactory : IUdpClientFactory
{
    public IUdpClient<ProtocolMessage> CreateClient(
        int port,
        bool enableEndianness,
        int maxPacketSize,
        IUdpObserver<ProtocolMessage> observer,
        ILogger logger
    )
    {
        UdpClient<ProtocolMessage> udpClient = new(
            new UdpSocket(port),
            new ProtocolMessageBinarySerializer
            {
                Network = enableEndianness,
            },
            observer,
            logger,
            maxPacketSize
        );

        return udpClient;
    }
}
