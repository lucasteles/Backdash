using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[StructLayout(LayoutKind.Sequential, Size = Size, Pack = 2)]
record struct Header(MsgType Type) : IBinarySerializable
{
    public MsgType Type = Type;
    public ushort Magic = 0;
    public ushort SequenceNumber = 0;

    public const int Size = 6;

    public readonly void Serialize(NetworkBufferWriter writer)
    {
        writer.Write((byte)Type);
        writer.Write(in Magic);
        writer.Write(in SequenceNumber);
    }

    public void Deserialize(NetworkBufferReader reader)
    {
        Type = (MsgType)reader.ReadByte();
        Magic = reader.ReadUShort();
        SequenceNumber = reader.ReadUShort();
    }
}
