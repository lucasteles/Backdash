using System.Runtime.InteropServices;
using Backdash.Serialization;

namespace Backdash.Network.Messages;

[Serializable]
[StructLayout(LayoutKind.Sequential, Size = Size, Pack = 2)]
record struct Header(MessageType Type)
{
    public MessageType Type = Type;
    public ushort Magic = 0;
    public ushort SequenceNumber = 0;
    public const int Size = 6;

    public readonly void Serialize(in BinaryRawBufferWriter writer)
    {
        writer.Write((ushort)Type);
        writer.Write(in Magic);
        writer.Write(in SequenceNumber);
    }

    public void Deserialize(in BinaryBufferReader reader)
    {
        try
        {
            Type = (MessageType)reader.ReadUInt16();
            Magic = reader.ReadUInt16();
            SequenceNumber = reader.ReadUInt16();
        }
        catch
        {
            Type = MessageType.Unknown;
            Magic = 0;
            SequenceNumber = 0;
        }
    }
}
