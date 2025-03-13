namespace Backdash.Serialization;

/// <summary>
///     Make the type Binary Serializable
/// </summary>
public interface IBinarySerializable
{
    /// <summary>
    ///     Serialize the current instance using <see cref="BinaryRawBufferWriter" />
    /// </summary>
    /// <param name="writer">Binary writer</param>
    void Serialize(ref readonly BinaryBufferWriter writer);

    /// <summary>
    ///     Deserialize the current instance using <see cref="BinaryBufferReader" />
    /// </summary>
    /// <param name="reader">Binary reader</param>
    void Deserialize(ref readonly BinaryBufferReader reader);
}
