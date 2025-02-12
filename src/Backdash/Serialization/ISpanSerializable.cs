using Backdash.Serialization.Buffer;

namespace Backdash.Serialization;

interface ISpanSerializable
{
    void Serialize(BinaryRawBufferWriter writer);
    void Deserialize(BinaryBufferReader reader);
}
