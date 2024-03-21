using Backdash.Network.Messages;
using Backdash.Serialization;

namespace Backdash.Network.Protocol.Comm;

sealed class ProtocolMessageBinarySerializer : SerializableTypeBinarySerializer<ProtocolMessage>
{
    public ProtocolMessageBinarySerializer(bool network = true) =>
        Endianness = Platform.GetEndianness(network);
}
