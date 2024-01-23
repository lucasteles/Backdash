using System.Runtime.InteropServices;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

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
