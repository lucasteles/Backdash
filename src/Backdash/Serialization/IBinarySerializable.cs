using Backdash.Serialization.Buffer;
namespace Backdash.Serialization;

interface IBinarySerializable
{
    void Serialize(BinarySpanWriter writer);
    void Deserialize(BinarySpanReader reader);
}
