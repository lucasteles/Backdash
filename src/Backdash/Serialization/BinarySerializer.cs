using Backdash.Serialization.Buffer;

namespace Backdash.Serialization;

public interface IBinaryReader<out T>
{
    T Deserialize(in ReadOnlySpan<byte> data);
}

public interface IBinaryWriter<T>
{
    int Serialize(ref T data, Span<byte> buffer);
}

public interface IBinarySerializer<T> : IBinaryReader<T>, IBinaryWriter<T>;

public abstract class BinarySerializer<T> : IBinarySerializer<T>
{
    public bool Network { get; init; } = true;

    protected abstract void Serialize(scoped BinaryBufferWriter writer, scoped in T data);

    protected abstract T Deserialize(scoped BinaryBufferReader reader);

    int IBinaryWriter<T>.Serialize(ref T data, Span<byte> buffer)
    {
        var offset = 0;
        BinaryBufferWriter writer = new(buffer, ref offset)
        {
            Network = Network,
        };
        Serialize(writer, in data);
        return offset;
    }

    T IBinaryReader<T>.Deserialize(in ReadOnlySpan<byte> data)
    {
        var offset = 0;
        BinaryBufferReader reader = new(data, ref offset)
        {
            Network = Network,
        };
        return Deserialize(reader);
    }
}
