using nGGPO.Serialization.Buffer;

namespace nGGPO.Serialization;

interface IBinarySerializable
{
    void Serialize(NetworkBufferWriter writer);
    void Deserialize(NetworkBufferReader reader);
}
