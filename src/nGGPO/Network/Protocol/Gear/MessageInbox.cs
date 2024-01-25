using System.Net;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;

namespace nGGPO.Network.Protocol.Gear;

sealed class MessageInbox(
    SocketAddress peer,
    UdpPeerClient<ProtocolMessage> udp
) : IDisposable
{
    ushort remoteMagicNumber;


    public void Dispose() { }
}
