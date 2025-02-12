using Backdash.Serialization.Buffer;

namespace Backdash.Serialization;

/// <summary>
/// Binary writer for <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">Type to be serialized.</typeparam>
public interface IBinaryBufferWriter<T>
{
    /// <summary>
    /// Serialize <paramref name="data"/> using <see cref="BinarySpanWriter"/>
    /// </summary>
    /// <param name="binaryWriter">Buffer writer</param>
    /// <param name="data">Data to be written</param>
    void Serialize(in BinaryBufferWriter binaryWriter, in T data);
}

/// <summary>
/// Binary reader for <typeparamref name="T"/>
/// </summary>
/// <typeparam name="T">Type to be deserialized.</typeparam>
public interface IBinaryBufferReader<T>
{
    /// <summary>
    /// Deserialize <paramref name="reader"/> into <paramref name="value"/>
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="value"></param>
    void Deserialize(in BinaryBufferReader reader, ref T value);
}

/// <summary>
/// State Serialization  Deserialization
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IStateSerializer<T> : IBinaryBufferWriter<T>, IBinaryBufferReader<T>;
