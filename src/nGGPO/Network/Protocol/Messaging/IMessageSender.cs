using nGGPO.Network.Messages;

namespace nGGPO.Network.Protocol.Messaging;

interface IMessageSender
{
    ValueTask SendMessage(ref ProtocolMessage msg, CancellationToken ct);

    bool TrySendMessage(ref ProtocolMessage msg);
}
