using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

interface IBinarySerializable
{
    void Serialize(NetworkBufferWriter writer);
    void Deserialize(NetworkBufferReader reader);
}
