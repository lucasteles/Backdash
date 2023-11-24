using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

struct QualityReply
{
    public uint Pong;

    public const int Size = sizeof(uint);

    public class Serializer : BinarySerializer<QualityReply>
    {
        public static readonly Serializer Instance = new();

        protected internal override void Serialize(
            scoped NetworkBufferWriter writer,
            in QualityReply data)
        {
            writer.Write(data.Pong);
        }

        protected internal override QualityReply Deserialize(scoped NetworkBufferReader reader) =>
            new()
            {
                Pong = reader.ReadUInt(),
            };
    }
}