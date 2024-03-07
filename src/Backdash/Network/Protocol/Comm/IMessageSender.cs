using Backdash.Network.Messages;
namespace Backdash.Network.Protocol.Comm;
interface IMessageSender
{
    ValueTask SendMessageAsync(in ProtocolMessage msg, CancellationToken ct);
    bool SendMessage(in ProtocolMessage msg);
}
