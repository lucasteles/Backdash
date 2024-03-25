using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using Backdash.Core;
using Backdash.Data;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[Serializable]
record struct InputMessage() : IBinarySerializable, IUtf8SpanFormattable
{
    public PeerStatusBuffer PeerConnectStatus = default;
    public Frame StartFrame = default;
    public bool DisconnectRequested = default;
    public Frame AckFrame = default;
    public ushort NumBits = default;
    public byte InputSize = default;
    public Memory<byte> Bits = Memory<byte>.Empty;

    public void Clear()
    {
        Mem.Clear(Bits.Span);
        PeerConnectStatus[..].Clear();
        StartFrame = Frame.Zero;
        DisconnectRequested = false;
        AckFrame = Frame.Zero;
        NumBits = 0;
        InputSize = 0;
    }

    public readonly void Serialize(BinarySpanWriter writer)
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
        writer.Write(Bits.Span[..bitCount]);
    }

    public void Deserialize(BinarySpanReader reader)
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

        var bits = Bits.Span;
        if (bits.Length is 0)
            bits = ArrayPool<byte>.Shared.Rent(bitCount);

        reader.ReadByte(bits[..bitCount]);
    }

    public readonly bool TryFormat(
        Span<byte> utf8Destination, out int bytesWritten,
        ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        bytesWritten = 0;
        using Utf8ObjectWriter writer = new(in utf8Destination, ref bytesWritten);
        if (!writer.Write(StartFrame)) return false;
        if (!writer.Write(AckFrame)) return false;
        if (!writer.Write(NumBits)) return false;
        return true;
    }
}

[Serializable, InlineArray(Max.NumberOfPlayers)]
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
