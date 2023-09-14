namespace nGGPO.Serialization;

public sealed class StructMarshalBinarySerializer<T> : IBinarySerializer<T> where T : struct
{
    public PooledBuffer Serialize(T message) =>
        Mem.SerializeMarshal(message);

    public T Deserialize(byte[] body) =>
        Mem.DeserializeMarshal<T>(body);
}

static class PrimitiveSerializers
{
}