using Backdash.Network;
using Backdash.Serialization.Buffer;

namespace Backdash.Serialization;

/// <summary>
/// Binary reader for <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">Type to be deserialized.</typeparam>
public interface IBinaryReader<T>
{
    /// <summary>
    /// Deserialize <paramref name="data"/> into <paramref name="value"/>
    /// </summary>
    /// <param name="data"></param>
    /// <param name="value"></param>
    int Deserialize(ReadOnlySpan<byte> data, ref T value);
}

/// <summary>
/// Binary writer for <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">Type to be serialized.</typeparam>
public interface IBinaryWriter<T>
{
    /// <summary>
    /// Serialize <paramref name="data"/> into <paramref name="buffer"/>
    /// </summary>
    /// <param name="data"></param>
    /// <param name="buffer"></param>
    int Serialize(in T data, Span<byte> buffer);
}

/// <summary>
/// Binary serializer for <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">Type to be serialized.</typeparam>
public interface IBinarySerializer<T> : IBinaryReader<T>, IBinaryWriter<T>;

/// <inheritdoc />
public abstract class BinarySerializer<T> : IBinarySerializer<T>
{
    /// <inheritdoc cref="NetcodeOptions.NetworkEndianness"/>
    public bool Network { get; init; } = true;

    /// <summary>
    /// Serialize <paramref name="data"/> using <see cref="BinaryRawBufferWriter"/>
    /// </summary>
    /// <param name="binaryWriter">Binary writer</param>
    /// <param name="data">Data to be written</param>
    protected abstract void Serialize(in BinaryRawBufferWriter binaryWriter, in T data);

    /// <summary>
    /// Deserialize buffer data using <paramref name="binaryReader"/> into <paramref name="result"/>
    /// </summary>
    /// <param name="binaryReader">Binary reader</param>
    /// <param name="result">Reference to be set with the deserialized value.</param>
    protected abstract void Deserialize(in BinaryBufferReader binaryReader, ref T result);

    int IBinaryWriter<T>.Serialize(in T data, Span<byte> buffer)
    {
        var offset = 0;
        BinaryRawBufferWriter writer = new(buffer, ref offset)
        {
            Endianness = Platform.GetEndianness(Network),
        };
        Serialize(in writer, in data);
        return offset;
    }

    int IBinaryReader<T>.Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        var offset = 0;
        BinaryBufferReader reader = new(data, ref offset)
        {
            Endianness = Platform.GetEndianness(Network),
        };
        Deserialize(in reader, ref value);
        return offset;
    }
}
