using Backdash.Network;

namespace Backdash.Serialization;

/// <summary>
///     Binary serializer for <typeparamref name="T" />
/// </summary>
/// <typeparam name="T">Type to be serialized.</typeparam>
public interface IBinarySerializer<T>
{
    /// <summary>
    ///     Get the <see cref="Endianness" /> used for serialization
    /// </summary>
    Endianness Endianness { get; }

    /// <summary>
    ///     Deserialize <paramref name="data" /> into <paramref name="value" />
    /// </summary>
    /// <param name="data"></param>
    /// <param name="value"></param>
    int Deserialize(ReadOnlySpan<byte> data, ref T value);

    /// <summary>
    ///     Serialize <paramref name="data" /> into <paramref name="buffer" />
    /// </summary>
    /// <param name="data"></param>
    /// <param name="buffer"></param>
    int Serialize(in T data, Span<byte> buffer);
}
