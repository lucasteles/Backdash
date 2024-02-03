using nGGPO.Serialization.Buffer;

namespace nGGPO.Serialization;

class SerializableTypeBinarySerializer<T> : IBinarySerializer<T>
    where T : struct, IBinarySerializable
{
    public bool Network { get; init; }

    public T Deserialize(in ReadOnlySpan<byte> data)
    {
        var offset = 0;
        NetworkBufferReader reader = new(data, ref offset)
        {
            Network = Network,
        };

        var msg = new T();
        msg.Deserialize(reader);
        return msg;
    }

    public int Serialize(ref T data, Span<byte> buffer)
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
