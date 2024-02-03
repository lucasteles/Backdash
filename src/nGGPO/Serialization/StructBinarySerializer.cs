using nGGPO.Core;

namespace nGGPO.Serialization;

sealed class StructBinarySerializer<T> : IBinarySerializer<T> where T : struct
{
    public T Deserialize(in ReadOnlySpan<byte> data) =>
        Mem.ReadStruct<T>(in data);

    public int Serialize(ref T data, Span<byte> buffer) =>
        Mem.WriteStruct(in data, buffer);
}

sealed class StructMarshalBinarySerializer<T> : IBinarySerializer<T> where T : struct
{
    public T Deserialize(in ReadOnlySpan<byte> data) => Mem.UnmarshallStruct<T>(in data);

    public int Serialize(ref T data, Span<byte> buffer) => Mem.MarshallStruct(in data, in buffer);
}
