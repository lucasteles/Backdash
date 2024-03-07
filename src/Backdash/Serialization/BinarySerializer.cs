using Backdash.Network;
using Backdash.Serialization.Buffer;
namespace Backdash.Serialization;
public interface IBinaryReader<T>
{
    int Deserialize(ReadOnlySpan<byte> data, ref T value);
}
public interface IBinaryWriter<T>
{
    int Serialize(in T data, Span<byte> buffer);
}
public interface IBinarySerializer<T> : IBinaryReader<T>, IBinaryWriter<T>;
public abstract class BinarySerializer<T> : IBinarySerializer<T>
{
    public bool Network { get; init; } = true;
    protected abstract void Serialize(in BinarySpanWriter writer, in T data);
    protected abstract void Deserialize(in BinarySpanReader reader, ref T result);
    int IBinaryWriter<T>.Serialize(in T data, Span<byte> buffer)
    {
        var offset = 0;
        BinarySpanWriter writer = new(buffer, ref offset)
        {
            Endianness = Platform.GetEndianness(Network),
        };
        Serialize(in writer, in data);
        return offset;
    }
    int IBinaryReader<T>.Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        var offset = 0;
        BinarySpanReader reader = new(data, ref offset)
        {
            Endianness = Platform.GetEndianness(Network),
        };
        Deserialize(in reader, ref value);
        return offset;
    }
}
