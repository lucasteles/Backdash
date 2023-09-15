using System;
using System.Buffers;
using nGGPO.Serialization.Buffer;
using nGGPO.Utils;

namespace nGGPO.Serialization;

public interface IBinarySerializer<T> where T : struct
{
    IMemoryOwner<byte> Serialize(T message);
    T Deserialize(ReadOnlySpan<byte> body);
}

public abstract class BinarySerializer<T> : IBinarySerializer<T> where T : struct
{
    public abstract int SizeOf(in T data);

    public bool Network { get; set; } = true;

    protected internal abstract void Serialize(ref NetworkBufferWriter writer, in T data);

    protected internal abstract T Deserialize(ref NetworkBufferReader reader);

    public IMemoryOwner<byte> Serialize(T data)
    {
        var buffer = Mem.Rent(SizeOf(in data));
        return Serialize(in data, in buffer);
    }

    public IMemoryOwner<byte> Serialize(in T data, in IMemoryOwner<byte> buffer, int offset = 0)
    {
        NetworkBufferWriter writer = new(buffer.Memory.Span, Network, offset);
        Serialize(ref writer, in data);
        return buffer;
    }

    public T Deserialize(ReadOnlySpan<byte> data)
    {
        NetworkBufferReader reader = new(data, Network);
        return Deserialize(ref reader);
    }
}