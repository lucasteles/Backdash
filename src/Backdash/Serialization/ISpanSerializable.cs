using Backdash.Serialization.Buffer;

namespace Backdash.Serialization;

interface ISpanSerializable
{
    void Serialize(BinarySpanWriter writer);
    void Deserialize(BinaryBufferReader reader);
}
