﻿using System.Runtime.InteropServices;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

[StructLayout(LayoutKind.Sequential, Size = Size)]
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
        sizeof(MsgType) + sizeof(ushort) * 2;

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