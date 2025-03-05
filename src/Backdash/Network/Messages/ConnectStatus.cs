using System.Runtime.InteropServices;
using Backdash.Data;
using Backdash.Serialization;

namespace Backdash.Network.Messages;

[Serializable, StructLayout(LayoutKind.Sequential)]
record struct ConnectStatus
{
    public bool Disconnected;
    public Frame LastFrame;

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
