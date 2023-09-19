using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

struct Header
{
    public MsgType Type;
    public ushort Magic;
    public ushort SequenceNumber;

    public Header(MsgType type)
    {
        Type = type;
        Magic = default;
        SequenceNumber = default;
    }

    public const int Size =
        sizeof(byte) + sizeof(ushort) + sizeof(ushort);

    public class Serializer : BinarySerializer<Header>
    {
        public static readonly Serializer Instance = new();

        public override int SizeOf(in Header data) => Size;

        protected internal override void Serialize(
            ref NetworkBufferWriter writer, in Header data)
        {
            writer.Write((byte) data.Type);
            writer.Write(data.Magic);
            writer.Write(data.SequenceNumber);
        }

        protected internal override Header Deserialize(ref NetworkBufferReader reader) =>
            new()
            {
                Type = (MsgType) reader.ReadByte(),
                Magic = reader.ReadUShort(),
                SequenceNumber = reader.ReadUShort(),
            };
    }
}