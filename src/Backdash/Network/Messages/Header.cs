using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;
namespace Backdash.Network.Messages;
[Serializable]
[StructLayout(LayoutKind.Sequential, Size = Size, Pack = 2)]
record struct Header(MessageType Type) : IBinarySerializable
{
    public MessageType Type = Type;
    public ushort Magic = 0;
    public ushort SequenceNumber = 0;
    public const int Size = 6;
    public readonly void Serialize(BinarySpanWriter writer)
    {
        writer.Write((byte)Type);
        writer.Write(in Magic);
        writer.Write(in SequenceNumber);
    }
    public void Deserialize(BinarySpanReader reader)
    {
        Type = (MessageType)reader.ReadByte();
        Magic = reader.ReadUShort();
        SequenceNumber = reader.ReadUShort();
    }
}
