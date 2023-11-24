using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

struct InputAck
{
    public int AckFrame;

    public const int Size = sizeof(int);
    public class Serializer : BinarySerializer<InputAck>
    {
        public static readonly Serializer Instance = new();

        protected internal override void Serialize(
            scoped NetworkBufferWriter writer,
            in InputAck data)
        {
            writer.Write(data.AckFrame);
        }

        protected internal override InputAck Deserialize(scoped NetworkBufferReader reader) =>
            new()
            {
                AckFrame = reader.ReadInt(),
            };
    }
}