using Backdash.Serialization.Buffer;

namespace Backdash.Serialization;

interface ISpanSerializable
{
    void Serialize(in BinaryRawBufferWriter writer);
    void Deserialize(in BinaryBufferReader reader);
}
