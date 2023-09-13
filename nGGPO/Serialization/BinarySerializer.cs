using System.Text.Json;

namespace nGGPO.Serialization;

public interface IBinarySerializer<T> where T : notnull
{
    ScopedBuffer Serialize(T message);
    T Deserialize(byte[] body);
}

public abstract class BinarySerializer<T> : IBinarySerializer<T> where T : notnull
{
    public abstract int SizeOf(T data);

    public bool Network { get; set; } = true;

    protected abstract void Serialize(ref NetworkBufferWriter writer, in T data);

    protected abstract T Deserialize(ref NetworkBufferReader reader);

    public ScopedBuffer Serialize(T data)
    {
        ScopedBuffer buffer = new(SizeOf(data));
        NetworkBufferWriter writer = new(buffer.Bytes, Network);
        Serialize(ref writer, in data);
        return buffer;
    }

    public T Deserialize(byte[] data)
    {
        NetworkBufferReader reader = new(data, Network);
        return Deserialize(ref reader);
    }
}

public class StructMarshalBinarySerializer<T> : IBinarySerializer<T> where T : notnull
{
    public ScopedBuffer Serialize(T message) =>
        Mem.SerializeMarshal(message);

    public T Deserialize(byte[] body) =>
        Mem.DeserializeMarshal<T>(body);
}

public class JsonBinarySerializer<T> : IBinarySerializer<T> where T : notnull
{
    readonly JsonSerializerOptions? options;

    public JsonBinarySerializer(JsonSerializerOptions? options = null) =>
        this.options = options;

    public ScopedBuffer Serialize(T message) =>
        new(JsonSerializer.SerializeToUtf8Bytes(message, options));

    public T Deserialize(byte[] body) =>
        JsonSerializer.Deserialize<T>(body, options)!;
}