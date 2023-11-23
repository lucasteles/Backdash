using System;
using nGGPO.Serialization.Buffer;
using nGGPO.Utils;

namespace nGGPO.Serialization;

public interface IBinarySerializer<T> where T : struct
{
    MemoryBuffer<byte> Serialize(T message);
    T Deserialize(ReadOnlySpan<byte> body);
}

public interface IBinarySerializer2<T> where T : struct
{
    int Serialize(T data, Span<byte> buffer);
    T Deserialize(in ReadOnlySpan<byte> data);
}

public abstract class BinarySerializer<T> : IBinarySerializer<T>, IBinarySerializer2<T>
    where T : struct
{
    public abstract int SizeOf(in T data);

    public bool Network { get; set; } = true;

    protected internal abstract void Serialize(ref NetworkBufferWriter writer, in T data);

    protected internal abstract T Deserialize(ref NetworkBufferReader reader);

    public MemoryBuffer<byte> Serialize(T data)
    {
        var buffer = Mem.Rent(SizeOf(in data));
        return Serialize(in data, in buffer);
    }

    public MemoryBuffer<byte> Serialize(in T data, in MemoryBuffer<byte> buffer, int offset = 0)
    {
        NetworkBufferWriter writer = new(buffer, Network, offset);
        Serialize(ref writer, in data);
        return buffer;
    }

    public T Deserialize(ReadOnlySpan<byte> data)
    {
        NetworkBufferReader reader = new(data, Network);
        return Deserialize(ref reader);
    }

    public int Serialize(T data, Span<byte> buffer)
    {
        NetworkBufferWriter writer = new(buffer, Network);
        Serialize(ref writer, in data);
        return writer.WrittenCount;
    }

    public T Deserialize(in ReadOnlySpan<byte> data)
    {
        NetworkBufferReader reader = new(data, Network);
        return Deserialize(ref reader);
    }
}