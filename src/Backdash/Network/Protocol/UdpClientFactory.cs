using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol.Comm;

namespace Backdash.Network.Protocol;

interface IUdpClientFactory
{
    IUdpClient<ProtocolMessage> CreateClient(
        int port,
        Endianness endianness,
        int maxPacketSize,
        bool useIPv6,
        IUdpObserver<ProtocolMessage> observer,
        Logger logger
    );
}

sealed class UdpClientFactory : IUdpClientFactory
{
    public IUdpClient<ProtocolMessage> CreateClient(
        int port,
        Endianness endianness,
        int maxPacketSize,
        bool useIPv6,
        IUdpObserver<ProtocolMessage> observer,
        Logger logger
    )
    {
        UdpClient<ProtocolMessage> udpClient = new(
            new UdpSocket(port, useIPv6),
            new ProtocolMessageBinarySerializer
            {
                Endianness = endianness,
            },
            observer,
            logger,
            maxPacketSize
        );
        return udpClient;
    }
}
