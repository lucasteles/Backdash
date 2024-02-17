using Backdash.Serialization.Buffer;

namespace Backdash.Serialization;

interface IBinarySerializable
{
    void Serialize(BinaryBufferWriter writer);
    void Deserialize(BinaryBufferReader reader);
}
