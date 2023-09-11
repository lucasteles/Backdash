using Backdash.Network.Messages;

namespace Backdash.Network.Protocol.Messaging;

interface IMessageSender
{
    ValueTask SendMessage(ref ProtocolMessage msg, CancellationToken ct);

    bool TrySendMessage(ref ProtocolMessage msg);
}
