using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

struct QualityReply
{
    public uint Pong;

    public const int Size = sizeof(uint);

    public void Serialize(NetworkBufferWriter writer) =>
        writer.Write(Pong);

    public void Deserialize(NetworkBufferReader reader) =>
        Pong = reader.ReadUInt();
}