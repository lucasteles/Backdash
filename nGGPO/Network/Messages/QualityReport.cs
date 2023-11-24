using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

struct QualityReport
{
    public byte FrameAdvantage; /* what's the other guy's frame advantage? */
    public uint Ping;

    public const int Size =
        sizeof(byte) + sizeof(uint);

    public class Serializer : BinarySerializer<QualityReport>
    {
        public static readonly Serializer Instance = new();

        protected internal override void Serialize(
            ref NetworkBufferWriter writer, in QualityReport data)
        {
            writer.Write(data.FrameAdvantage);
            writer.Write(data.Ping);
        }

        protected internal override QualityReport Deserialize(ref NetworkBufferReader reader) =>
            new()
            {
                FrameAdvantage = reader.ReadByte(),
                Ping = reader.ReadUInt(),
            };
    }
}