using nGGPO.Network;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Serialization;

class UdpMsgBinarySerializer : IBinarySerializer<UdpMsg>
{
    public bool Network { get; init; }

    public UdpMsg Deserialize(in ReadOnlySpan<byte> data)
    {
        var offset = 0;
        NetworkBufferReader reader = new(data, ref offset)
        {
            Network = Network,
        };

        var msg = new UdpMsg();
        msg.Deserialize(reader);
        return msg;
    }

    public int Serialize(ref UdpMsg data, Span<byte> buffer)
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
