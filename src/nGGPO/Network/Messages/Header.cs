using System.Runtime.InteropServices;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

[StructLayout(LayoutKind.Sequential, Size = Size)]
record struct Header(MsgType Type) : IBinarySerializable
{
    public MsgType Type = Type;
    public ushort Magic;
    public ushort SequenceNumber;

    public const int Size = sizeof(byte) + sizeof(ushort) + sizeof(ushort);

    public readonly void Serialize(NetworkBufferWriter writer)
    {
        writer.Write((byte)Type);
        writer.Write(Magic);
        writer.Write(SequenceNumber);
    }

    public void Deserialize(NetworkBufferReader reader)
    {
        Type = (MsgType)reader.ReadByte();
        Magic = reader.ReadUShort();
        SequenceNumber = reader.ReadUShort();
    }
}
