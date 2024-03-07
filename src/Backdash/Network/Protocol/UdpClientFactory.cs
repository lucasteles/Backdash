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
        IUdpObserver<ProtocolMessage> observer,
        Logger logger
    )
    {
        UdpClient<ProtocolMessage> udpClient = new(
            new UdpSocket(port),
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
