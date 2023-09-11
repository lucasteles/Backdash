using Backdash.Serialization.Buffer;

namespace Backdash.Serialization;

interface IBinarySerializable
{
    void Serialize(NetworkBufferWriter writer);
    void Deserialize(NetworkBufferReader reader);
}
