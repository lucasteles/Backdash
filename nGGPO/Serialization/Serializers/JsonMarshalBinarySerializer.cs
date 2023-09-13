using System.Runtime.InteropServices;

namespace nGGPO.Serialization.Serializers;

public class StructMarshalBinarySerializer : IBinarySerializer
{
    public ScopedBuffer Serialize<T>(T message) where T : notnull =>
        Mem.SerializeMarshal(message);

    public T Deserialize<T>(byte[] body) where T : notnull =>
        Mem.DeserializeMarshal<T>(body);
}

public class StructMarshalBinarySerializer<T> : IBinarySerializer<T> where T : notnull
{
    public int Size(T message) => Marshal.SizeOf(message);

    public ScopedBuffer Serialize(T message) =>
        Mem.SerializeMarshal(message);

    public T Deserialize(byte[] body) =>
        Mem.DeserializeMarshal<T>(body);
}