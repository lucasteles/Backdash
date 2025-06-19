using Backdash.Network;
using Backdash.Serialization;

namespace Backdash.Tests.TestUtils.Types;

/// <inheritdoc />
abstract class BaseBinarySerializer<T>(Endianness endianness = Endianness.BigEndian) : IBinarySerializer<T>
{
    /// <inheritdoc />
    public Endianness Endianness { get; } = endianness;

    /// <summary>
    /// Serialize <paramref name="data"/> using <see cref="BinarySpanWriter"/>
    /// </summary>
    /// <param name="binaryWriter">Binary writer</param>
    /// <param name="data">Data to be written</param>
    protected abstract void Serialize(in BinarySpanWriter binaryWriter, in T data);

    /// <summary>
    /// Deserialize buffer data using <paramref name="binaryReader"/> into <paramref name="result"/>
    /// </summary>
    /// <param name="binaryReader">Binary reader</param>
    /// <param name="result">Reference to be set with the deserialized value.</param>
    protected abstract void Deserialize(in BinaryBufferReader binaryReader, ref T result);

    int IBinarySerializer<T>.Serialize(in T data, Span<byte> buffer)
    {
        var offset = 0;
        BinarySpanWriter writer = new(buffer, ref offset, Endianness);
        Serialize(in writer, in data);
        return writer.WrittenCount;
    }

    int IBinarySerializer<T>.Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        var offset = 0;
        BinaryBufferReader reader = new(data, ref offset, Endianness);
        Deserialize(in reader, ref value);
        return offset;
    }
}
