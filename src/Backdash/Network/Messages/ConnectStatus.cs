using Backdash.Data;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

record struct ConnectStatus : IBinarySerializable
{
    public bool Disconnected;
    public Frame LastFrame;

    public readonly void Serialize(NetworkBufferWriter writer)
    {
        writer.Write(in Disconnected);
        writer.Write(in LastFrame.Number);
    }

    public void Deserialize(NetworkBufferReader reader)
    {
        Disconnected = reader.ReadBool();
        LastFrame = new(reader.ReadInt());
    }
}
