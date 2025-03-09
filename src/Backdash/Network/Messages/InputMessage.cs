using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Backdash.Core;
using Backdash.Data;
using Backdash.Serialization;
using Backdash.Serialization.Internal;

namespace Backdash.Network.Messages;

[Serializable, StructLayout(LayoutKind.Sequential)]
struct InputMessage : IEquatable<InputMessage>, IUtf8SpanFormattable
{
    public byte PeerCount;
    public PeerStatusBuffer PeerConnectStatus;
    public Frame StartFrame;
    public bool DisconnectRequested;
    public Frame AckFrame;
    public byte InputSize;
    public ushort NumBits;
    public InputMessageBuffer Bits;

    public void Clear()
    {
        Mem.Clear(Bits);
        PeerCount = 0;
        ((Span<ConnectStatus>)PeerConnectStatus).Clear();
        StartFrame = Frame.Zero;
        DisconnectRequested = false;
        AckFrame = Frame.Zero;
        NumBits = 0;
        InputSize = 0;
    }

    public readonly void Serialize(in BinaryRawBufferWriter writer)
    {
        writer.Write(in StartFrame);
        writer.Write(in AckFrame);
        writer.Write(in DisconnectRequested);
        writer.Write(in PeerCount);

        ReadOnlySpan<ConnectStatus> peerStatuses = PeerConnectStatus;
        ref var currentPeerStatus = ref MemoryMarshal.GetReference(peerStatuses);
        ref var limitPeerStatus = ref Unsafe.Add(ref currentPeerStatus, PeerCount);
        while (Unsafe.IsAddressLessThan(ref currentPeerStatus, ref limitPeerStatus))
        {
            currentPeerStatus.Serialize(writer);
            currentPeerStatus = ref Unsafe.Add(ref currentPeerStatus, 1)!;
        }

        writer.Write(in InputSize);
        writer.Write(in NumBits);
        writer.Write(Bits[..ByteSize.ByteCountOfBits(in NumBits)]);
    }

    public void Deserialize(in BinaryBufferReader reader)
    {
        StartFrame = reader.ReadAsInt32<Frame>();
        AckFrame = reader.ReadAsInt32<Frame>();
        DisconnectRequested = reader.ReadBoolean();
        PeerCount = reader.ReadByte();

        Span<ConnectStatus> peerStatuses = PeerConnectStatus;
        ref var currentPeerStatus = ref MemoryMarshal.GetReference(peerStatuses);
        ref var limitPeerStatus = ref Unsafe.Add(ref currentPeerStatus, PeerCount);
        while (Unsafe.IsAddressLessThan(ref currentPeerStatus, ref limitPeerStatus))
        {
            currentPeerStatus.Deserialize(reader);
            currentPeerStatus = ref Unsafe.Add(ref currentPeerStatus, 1)!;
        }

        InputSize = reader.ReadByte();
        NumBits = reader.ReadUInt16();
        reader.Read(Bits[..ByteSize.ByteCountOfBits(in NumBits)]);
    }

    public readonly bool TryFormat(
        Span<byte> utf8Destination, out int bytesWritten,
        ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        bytesWritten = 0;
        using Utf8ObjectWriter writer = new(in utf8Destination, ref bytesWritten);
        return writer.Write(in StartFrame) && writer.Write(in AckFrame) && writer.Write(in NumBits);
    }

    public readonly bool Equals(in InputMessage other) =>
        StartFrame.Equals(other.StartFrame) &&
        DisconnectRequested == other.DisconnectRequested &&
        AckFrame.Equals(other.AckFrame) && NumBits == other.NumBits &&
        InputSize == other.InputSize &&
        PeerConnectStatus.Equals(other.PeerConnectStatus) &&
        Bits.Equals(other.Bits);

    public override readonly bool Equals(object? obj) => obj is InputMessage other && Equals(in other);

    readonly bool IEquatable<InputMessage>.Equals(InputMessage other) => Equals(in other);

    public override readonly int GetHashCode() => HashCode.Combine(
        PeerConnectStatus, StartFrame, DisconnectRequested, AckFrame, NumBits, InputSize, Bits);

    public static bool operator ==(in InputMessage left, in InputMessage right) => left.Equals(in right);
    public static bool operator !=(in InputMessage left, in InputMessage right) => !left.Equals(in right);
}

[Serializable, InlineArray(Max.NumberOfPlayers)]
struct PeerStatusBuffer : IEquatable<PeerStatusBuffer>
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

    public override readonly int GetHashCode() => Mem.GetHashCode<ConnectStatus>(this);
    public readonly bool Equals(PeerStatusBuffer other) => this[..].SequenceEqual(other);
    public override readonly bool Equals(object? obj) => obj is PeerStatusBuffer other && Equals(other);
}

[Serializable, InlineArray(Max.CompressedBytes)]
struct InputMessageBuffer : IEquatable<InputMessageBuffer>
{
    byte element0;
    public InputMessageBuffer(ReadOnlySpan<byte> bits) => bits.CopyTo(this);

    public override readonly string ToString() => Mem.GetBitString(this);
    public override readonly int GetHashCode() => Mem.GetHashCode<byte>(this);

    public readonly bool Equals(in InputMessageBuffer other) => Mem.EqualBytes(this, other, truncate: true);

    public override readonly bool Equals(object? obj) => obj is InputMessageBuffer other && Equals(other);

    readonly bool IEquatable<InputMessageBuffer>.Equals(InputMessageBuffer other) => Equals(in other);
}
