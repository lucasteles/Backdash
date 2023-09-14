using nGGPO.Serialization;

namespace nGGPO.Network.Messages;

struct QualityReply
{
    public uint Pong;

    public const int Size = sizeof(uint);

    public class Serializer : BinarySerializer<QualityReply>
    {
        public static readonly Serializer Instance = new();

        public override int SizeOf(in QualityReply data) => Size;

        protected internal override void Serialize(
            ref NetworkBufferWriter writer,
            in QualityReply data)
        {
            writer.Write(data.Pong);
        }

        protected internal override QualityReply Deserialize(ref NetworkBufferReader reader) =>
            new()
            {
                Pong = reader.ReadUInt(),
            };
    }
}