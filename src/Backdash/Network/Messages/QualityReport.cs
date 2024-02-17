using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct QualityReport : IBinarySerializable
{
    public int FrameAdvantage; /* what's the other guy's frame advantage? */
    public long Ping;

    public readonly void Serialize(BinaryBufferWriter writer)
    {
        writer.Write(in FrameAdvantage);
        writer.Write(in Ping);
    }

    public void Deserialize(BinaryBufferReader reader)
    {
        FrameAdvantage = reader.ReadInt();
        Ping = reader.ReadLong();
    }
}
