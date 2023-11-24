using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

struct SyncReply
{
    public uint RandomReply; /* please reply back with this random data */

    public void Serialize(NetworkBufferWriter writer) => writer.Write(RandomReply);

    public void Deserialize(NetworkBufferReader reader) => RandomReply = reader.ReadUInt();
}