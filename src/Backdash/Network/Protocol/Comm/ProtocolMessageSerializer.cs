using Backdash.Network.Messages;
using Backdash.Serialization;

namespace Backdash.Network.Protocol.Comm;

sealed class ProtocolMessageSerializer(Endianness endianness) : IBinarySerializer<ProtocolMessage>
{
    public Endianness Endianness => endianness;

    public int Serialize(in ProtocolMessage data, Span<byte> buffer)
    {
        var offset = 0;
        BinarySpanWriter writer = new(buffer, ref offset, endianness);
        data.Serialize(in writer);
        return writer.WrittenCount;
    }

    public int Deserialize(ReadOnlySpan<byte> data, ref ProtocolMessage value)
    {
        var offset = 0;
        BinaryBufferReader reader = new(data, ref offset, endianness);
        value.Deserialize(in reader);
        return offset;
    }
}
