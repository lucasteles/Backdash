using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct QualityReport : IBinarySerializable
{
    public byte FrameAdvantage; /* what's the other guy's frame advantage? */
    public uint Ping;

    public readonly void Serialize(NetworkBufferWriter writer)
    {
        writer.Write(FrameAdvantage);
        writer.Write(Ping);
    }

    public void Deserialize(NetworkBufferReader reader)
    {
        FrameAdvantage = reader.ReadByte();
        Ping = reader.ReadUInt();
    }
}
