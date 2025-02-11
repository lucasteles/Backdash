using System.Runtime.InteropServices;
using Backdash.Data;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[Serializable, StructLayout(LayoutKind.Sequential)]
record struct ConnectStatus : ISpanSerializable
{
    public bool Disconnected;
    public Frame LastFrame;

    public readonly void Serialize(BinarySpanWriter writer)
    {
        writer.Write(in Disconnected);
        writer.Write(in LastFrame.Number);
    }

    public void Deserialize(BinarySpanReader reader)
    {
        Disconnected = reader.ReadBoolean();
        LastFrame = new(reader.ReadInt32());
    }
}
