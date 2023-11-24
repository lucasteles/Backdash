using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

struct ConnectStatus
{
    public bool Disconnected;
    public int LastFrame;

    public const int Size = sizeof(bool) + sizeof(int);

    public void Serialize(NetworkBufferWriter writer)
    {
        writer.Write(Disconnected);
        writer.Write(LastFrame);
    }

    public void Deserialize(NetworkBufferReader reader)
    {
        Disconnected = reader.ReadBool();
        LastFrame = reader.ReadInt();
    }
}