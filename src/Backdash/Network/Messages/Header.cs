using System.Runtime.InteropServices;
using Backdash.Serialization;

namespace Backdash.Network.Messages;

[Serializable]
[StructLayout(LayoutKind.Sequential, Size = Size)]
record struct Header(MessageType Type)
{
    public const int Size = 6;

    public MessageType Type = Type;
    public ushort SyncNumber = 0;
    public ushort SequenceNumber = 0;

    public readonly void Serialize(in BinarySpanWriter writer)
    {
        writer.Write((ushort)Type);
        writer.Write(in SyncNumber);
        writer.Write(in SequenceNumber);
    }

    public void Deserialize(in BinaryBufferReader reader)
    {
        try
        {
            Type = reader.ReadAsUInt16<MessageType>();
            SyncNumber = reader.ReadUInt16();
            SequenceNumber = reader.ReadUInt16();
        }
        catch
        {
            Type = MessageType.Unknown;
            SyncNumber = 0;
            SequenceNumber = 0;
        }
    }
}
