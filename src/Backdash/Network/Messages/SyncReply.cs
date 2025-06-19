using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Internal;

namespace Backdash.Network.Messages;

[Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
record struct SyncReply : IUtf8SpanFormattable
{
    public uint RandomReply; /* please reply with this random data */
    public long Pong;

    public readonly void Serialize(in BinarySpanWriter writer)
    {
        writer.Write(in RandomReply);
        writer.Write(in Pong);
    }

    public void Deserialize(in BinaryBufferReader reader)
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
        return writer.Write(in RandomReply) && writer.Write(in Pong);
    }
}
