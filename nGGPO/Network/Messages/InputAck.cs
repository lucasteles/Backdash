using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

struct InputAck
{
    public int AckFrame;

    public const int Size = sizeof(int);

    public void Serialize(NetworkBufferWriter writer) => writer.Write(AckFrame);

    public void Deserialize(NetworkBufferReader reader) =>
        AckFrame = reader.ReadInt();
}