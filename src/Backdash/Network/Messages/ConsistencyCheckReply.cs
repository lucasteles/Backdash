using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Internal;

namespace Backdash.Network.Messages;

[Serializable, StructLayout(LayoutKind.Sequential)]
record struct ConsistencyCheckReply : IUtf8SpanFormattable
{
    public Frame Frame;
    public uint Checksum;

    public readonly void Serialize(in BinaryRawBufferWriter writer)
    {
        writer.Write(in Frame);
        writer.Write(in Checksum);
    }

    public void Deserialize(in BinaryBufferReader reader)
    {
        Frame = reader.ReadFrame();
        Checksum = reader.ReadUInt32();
    }

    public readonly bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        using Utf8ObjectWriter writer = new(in utf8Destination, ref bytesWritten);
        return writer.Write(in Frame) && writer.Write(in Checksum);
    }
}
