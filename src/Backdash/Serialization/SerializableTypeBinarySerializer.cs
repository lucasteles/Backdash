using System.Runtime.CompilerServices;
using Backdash.Network;
using Backdash.Serialization.Buffer;
namespace Backdash.Serialization;

class SerializableTypeBinarySerializer<T> : IBinarySerializer<T>
    where T : struct, ISpanSerializable
{
    public Endianness Endianness { get; init; }
    public int Serialize(in T data, Span<byte> buffer)
    {
        var offset = 0;
        BinaryRawBufferWriter writer = new(buffer, ref offset)
        {
            Endianness = Endianness,
        };
        ref var dataRef = ref Unsafe.AsRef(in data);
        dataRef.Serialize(writer);
        return offset;
    }
    public int Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        var offset = 0;
        BinaryBufferReader reader = new(data, ref offset)
        {
            Endianness = Endianness,
        };
        value.Deserialize(reader);
        return offset;
    }
}
