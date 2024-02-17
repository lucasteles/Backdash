using System.Runtime.CompilerServices;
using System.Text;
using Backdash.Core;
using Backdash.Data;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[Serializable]
record struct InputMessage : IBinarySerializable
{
    public PeerStatusBuffer PeerConnectStatus;
    public Frame StartFrame;
    public bool DisconnectRequested;
    public Frame AckFrame;
    public ushort NumBits;
    public byte InputSize;
    public InputMessageBuffer Bits;

    public void Clear()
    {
        Mem.Clear(Bits);
        PeerConnectStatus[..].Clear();
        StartFrame = Frame.Zero;
        DisconnectRequested = false;
        AckFrame = Frame.Zero;
        NumBits = 0;
        InputSize = 0;
    }

    public readonly void Serialize(scoped BinaryBufferWriter writer)
    {
        ReadOnlySpan<ConnectStatus> peerStatuses = PeerConnectStatus;
        var peerCount = (byte)peerStatuses.Length;
        writer.Write(peerCount);
        for (var i = 0; i < peerCount; i++)
            peerStatuses[i].Serialize(writer);

        writer.Write(in StartFrame.Number);
        writer.Write(in DisconnectRequested);
        writer.Write(in AckFrame.Number);
        writer.Write(in InputSize);
        writer.Write(in NumBits);
        var bitCount = (int)Math.Ceiling(NumBits / (float)ByteSize.ByteToBits);
        writer.Write(Bits, bitCount);
    }

    public void Deserialize(scoped BinaryBufferReader reader)
    {
        var peerCount = reader.ReadByte();
        for (var i = 0; i < peerCount; i++)
            PeerConnectStatus[i].Deserialize(reader);

        StartFrame = new(reader.ReadInt());
        DisconnectRequested = reader.ReadBool();
        AckFrame = new(reader.ReadInt());
        InputSize = reader.ReadByte();
        NumBits = reader.ReadUShort();
        var bitCount = (int)Math.Ceiling(NumBits / (float)ByteSize.ByteToBits);
        reader.ReadByte(Bits, bitCount);
    }
}

[Serializable, InlineArray(Max.RemoteConnections)]
struct PeerStatusBuffer
{
    ConnectStatus element0;

    public PeerStatusBuffer(ReadOnlySpan<ConnectStatus> buffer) => buffer.CopyTo(this);

    public override readonly string ToString()
    {
        ReadOnlySpan<ConnectStatus> values = this;
        StringBuilder builder = new();
        builder.Append('[');

        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0)
                builder.Append(", ");

            ref readonly var curr = ref values[i];
            if (curr.LastFrame.IsNull)
                builder.Append("OFF");
            else
            {
                builder.Append(curr.Disconnected ? "CLOSED" : "OK");
                builder.Append('(');
                builder.Append(curr.LastFrame);
                builder.Append(')');
            }
        }

        builder.Append(']');
        return builder.ToString();
    }
}

[Serializable, InlineArray(Max.CompressedBytes)]
public struct InputMessageBuffer
{
    byte element0;
    public InputMessageBuffer(ReadOnlySpan<byte> bits) => bits.CopyTo(this);

    public override readonly string ToString() =>
        Mem.GetBitString(this, splitAt: Max.InputSizeInBytes);
}
