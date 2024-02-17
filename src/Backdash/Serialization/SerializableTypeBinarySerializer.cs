using Backdash.Serialization.Buffer;

namespace Backdash.Serialization;

class SerializableTypeBinarySerializer<T> : IBinarySerializer<T>
    where T : struct, IBinarySerializable
{
    public bool Network { get; init; }

    public T Deserialize(in ReadOnlySpan<byte> data)
    {
        var offset = 0;
        BinaryBufferReader reader = new(data, ref offset)
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
        BinaryBufferWriter writer = new(buffer, ref offset)
        {
            Network = Network,
        };
        data.Serialize(writer);
        return offset;
    }
}
