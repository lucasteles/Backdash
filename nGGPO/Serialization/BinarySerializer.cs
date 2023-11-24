using System;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Serialization;

public interface IBinaryReader<out T> where T : struct
{
    T Deserialize(in ReadOnlySpan<byte> data);
}

public interface IBinaryWriter<T> where T : struct
{
    int Serialize(in T data, Span<byte> buffer);
}

public interface IBinarySerializer<T> : IBinaryReader<T>, IBinaryWriter<T> where T : struct;

public abstract class BinarySerializer<T> : IBinarySerializer<T>
    where T : struct
{

    public bool Network { get; init; } = true;

    protected internal abstract void Serialize(ref NetworkBufferWriter writer, in T data);

    protected internal abstract T Deserialize(ref NetworkBufferReader reader);

    public int Serialize(in T data, Span<byte> buffer)
    {
        NetworkBufferWriter writer = new(ref buffer) {Network = Network};
        Serialize(ref writer, in data);
        return writer.WrittenCount;
    }

    public T Deserialize(in ReadOnlySpan<byte> data)
    {
        NetworkBufferReader reader = new(data, Network);
        return Deserialize(ref reader);
    }
}