using Backdash.Network.Messages;

namespace Backdash.Network.Protocol.Comm;

interface IMessageSender
{
    bool SendMessage(in ProtocolMessage msg);
}
