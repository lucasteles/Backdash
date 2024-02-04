using nGGPO.Data;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

record struct ConnectStatus : IBinarySerializable
{
    public bool Disconnected;
    public Frame LastFrame;

    public readonly void Serialize(NetworkBufferWriter writer)
    {
        writer.Write(Disconnected);
        writer.Write(LastFrame.Number);
    }

    public void Deserialize(NetworkBufferReader reader)
    {
        Disconnected = reader.ReadBool();
        LastFrame = new(reader.ReadInt());
    }
}
