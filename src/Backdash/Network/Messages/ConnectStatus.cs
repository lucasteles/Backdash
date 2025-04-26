using Backdash.Serialization;

namespace Backdash.Network.Messages;

[Serializable]
record struct ConnectStatus
{
    public Frame LastFrame;
    public bool Disconnected;

    public readonly void Serialize(in BinaryRawBufferWriter writer)
    {
        writer.Write(in Disconnected);
        writer.Write(in LastFrame);
    }

    public void Deserialize(in BinaryBufferReader reader)
    {
        Disconnected = reader.ReadBoolean();
        LastFrame = reader.ReadFrame();
    }
}
