using System.Runtime.CompilerServices;
using System.Text;
using Backdash.Core;
using Backdash.Data;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[Serializable]
record struct InputMsg : IBinarySerializable
{
    public byte PeerCount;
    public PeerStatusBuffer PeerConnectStatus;
    public Frame StartFrame;
    public bool DisconnectRequested;
    public Frame AckFrame;
    public ushort NumBits;
    public byte InputSize;
    public InputMsgBuffer Bits;

    public void Clear()
    {
        Mem.Clear(Bits);
        PeerConnectStatus[..].Clear();
        PeerCount = 0;
        StartFrame = Frame.Zero;
        DisconnectRequested = false;
        AckFrame = Frame.Zero;
        NumBits = 0;
        InputSize = 0;
    }

    public readonly void Serialize(scoped NetworkBufferWriter writer)
    {
        writer.Write(PeerCount);
        for (var i = 0; i < PeerCount; i++)
            PeerConnectStatus[i].Serialize(writer);

        writer.Write(StartFrame.Number);
        writer.Write(DisconnectRequested);
        writer.Write(AckFrame.Number);
        writer.Write(NumBits);
        writer.Write(InputSize);
        writer.Write(Bits, InputSize);
    }

    public void Deserialize(scoped NetworkBufferReader reader)
    {
        PeerCount = reader.ReadByte();
        for (var i = 0; i < PeerCount; i++)
            PeerConnectStatus[i].Deserialize(reader);

        StartFrame = new(reader.ReadInt());
        DisconnectRequested = reader.ReadBool();
        AckFrame = new(reader.ReadInt());
        NumBits = reader.ReadUShort();
        InputSize = reader.ReadByte();

        reader.ReadByte(Bits, InputSize);
    }
}

[Serializable, InlineArray(Max.MsgPlayers)]
struct PeerStatusBuffer
{
    ConnectStatus element0;

    public PeerStatusBuffer(ReadOnlySpan<ConnectStatus> buffer) => buffer.CopyTo(this);

    public override readonly string ToString()
    {
        ReadOnlySpan<ConnectStatus> values = this;
        StringBuilder builder = new();
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
                builder.Append(',');

            var curr = values[i];

            builder.Append(curr.Disconnected ? "ON" : "OFF");
            builder.Append('(');
            builder.Append(curr.LastFrame);
            builder.Append(')');
            builder.Append(' ');
        }

        return builder.ToString();
    }
}

[Serializable, InlineArray(Max.CompressedBytes)]
public struct InputMsgBuffer
{
    byte element0;
    public InputMsgBuffer(ReadOnlySpan<byte> bits) => bits.CopyTo(this);
}
