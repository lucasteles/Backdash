using nGGPO.Network.Messages;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Serialization;

class UdpMsgBinarySerializer : IBinarySerializer<ProtocolMessage>
{
    public bool Network { get; init; }

    public ProtocolMessage Deserialize(in ReadOnlySpan<byte> data)
    {
        var offset = 0;
        NetworkBufferReader reader = new(data, ref offset)
        {
            Network = Network,
        };

        var msg = new ProtocolMessage();
        msg.Deserialize(reader);
        return msg;
    }

    public int Serialize(ref ProtocolMessage data, Span<byte> buffer)
    {
        var offset = 0;
        NetworkBufferWriter writer = new(buffer, ref offset)
        {
            Network = Network,
        };
        data.Serialize(writer);
        return offset;
    }
}
