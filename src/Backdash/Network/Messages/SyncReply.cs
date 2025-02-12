using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[Serializable, StructLayout(LayoutKind.Sequential)]
record struct SyncReply : ISpanSerializable, IUtf8SpanFormattable
{
    public uint RandomReply; /* please reply with this random data */
    public long Pong;

    public readonly void Serialize(BinarySpanWriter writer)
    {
        writer.Write(in RandomReply);
        writer.Write(in Pong);
    }

    public void Deserialize(BinaryBufferReader reader)
    {
        RandomReply = reader.ReadUInt32();
        Pong = reader.ReadInt64();
    }

    public readonly bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        using Utf8ObjectWriter writer = new(in utf8Destination, ref bytesWritten);
        if (!writer.Write(RandomReply)) return false;
        if (!writer.Write(Pong)) return false;
        return true;
    }
}
