using System.Runtime.InteropServices;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

[StructLayout(LayoutKind.Sequential, Size = Size)]
struct Header(MsgType type)
{
    public MsgType Type = type;
    public ushort Magic;
    public ushort SequenceNumber;

    public const int Size = sizeof(byte) + sizeof(ushort) + sizeof(ushort);

    public void Serialize(NetworkBufferWriter writer)
    {
        writer.Write((byte) Type);
        writer.Write(Magic);
        writer.Write(SequenceNumber);
    }

    public void Deserialize(NetworkBufferReader reader)
    {
        Type = (MsgType) reader.ReadByte();
        Magic = reader.ReadUShort();
        SequenceNumber = reader.ReadUShort();
    }
}