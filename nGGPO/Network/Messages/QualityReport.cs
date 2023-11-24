using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

struct QualityReport
{
    public byte FrameAdvantage; /* what's the other guy's frame advantage? */
    public uint Ping;

    public void Serialize(NetworkBufferWriter writer)
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