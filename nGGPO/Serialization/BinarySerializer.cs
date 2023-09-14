namespace nGGPO.Serialization;

public interface IBinarySerializer<T> where T : struct
{
    PooledBuffer Serialize(T message);
    T Deserialize(byte[] body);
}

public abstract class BinarySerializer<T> : IBinarySerializer<T> where T : struct
{
    public abstract int SizeOf(T data);

    public bool Network { get; set; } = true;

    protected internal abstract void Serialize(ref NetworkBufferWriter writer, in T data);

    protected internal abstract T Deserialize(ref NetworkBufferReader reader);

    public PooledBuffer Serialize(T data)
    {
        var buffer = Mem.CreateBuffer(SizeOf(data));
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