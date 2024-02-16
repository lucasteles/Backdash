using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct QualityReport : IBinarySerializable
{
    public int FrameAdvantage; /* what's the other guy's frame advantage? */
    public long Ping;

    public readonly void Serialize(NetworkBufferWriter writer)
    {
        writer.Write(in FrameAdvantage);
        writer.Write(in Ping);
    }

    public void Deserialize(NetworkBufferReader reader)
    {
        FrameAdvantage = reader.ReadInt();
        Ping = reader.ReadLong();
    }
}
