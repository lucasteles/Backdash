using nGGPO.Network.Messages;

namespace nGGPO.Network.Protocol.Internal;

interface IMessageSender
{
    ValueTask SendMessage(ref ProtocolMessage msg, CancellationToken ct);
}
