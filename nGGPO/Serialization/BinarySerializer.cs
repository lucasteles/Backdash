using System;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Serialization;

public interface IBinaryReader<out T> where T : struct
{
    T Deserialize(in ReadOnlySpan<byte> data);
}

public interface IBinaryWriter<T> where T : struct
{
    int Serialize(ref T data, Span<byte> buffer);
}

public interface IBinarySerializer<T> : IBinaryReader<T>, IBinaryWriter<T> where T : struct;

public abstract class BinarySerializer<T> : IBinarySerializer<T>
    where T : struct
{
    public bool Network { get; init; } = true;

    protected internal abstract void Serialize(scoped NetworkBufferWriter writer, scoped in T data);

    protected internal abstract T Deserialize(scoped NetworkBufferReader reader);

    public int Serialize(ref T data, Span<byte> buffer)
    {
        var offset = 0;
        NetworkBufferWriter writer = new(buffer, ref offset) {Network = Network};
        Serialize(writer, in data);
        return offset;
    }

    public T Deserialize(in ReadOnlySpan<byte> data)
    {
        var offset = 0;
        NetworkBufferReader reader = new(data, ref offset) {Network = Network};
        return Deserialize(reader);
    }
}