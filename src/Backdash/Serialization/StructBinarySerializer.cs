using Backdash.Core;

namespace Backdash.Serialization;

sealed class StructBinarySerializer<T> : IBinarySerializer<T> where T : struct
{
    public int Serialize(in T data, Span<byte> buffer) => Mem.WriteStruct(in data, buffer);

    public void Deserialize(ReadOnlySpan<byte> data, ref T value) => value = Mem.ReadStruct<T>(in data);
}

sealed class StructMarshalBinarySerializer<T> : IBinarySerializer<T> where T : struct
{
    public int Serialize(in T data, Span<byte> buffer) => Mem.MarshallStruct(in data, in buffer);

    public void Deserialize(ReadOnlySpan<byte> data, ref T value) => value = Mem.UnmarshallStruct<T>(in data);
}
