using System.Runtime.InteropServices;
using Backdash.Data;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[Serializable, StructLayout(LayoutKind.Sequential)]
record struct ConsistencyCheckReply : ISpanSerializable, IUtf8SpanFormattable
{
    public Frame Frame;
    public int Checksum;

    public readonly void Serialize(in BinaryRawBufferWriter writer)
    {
        writer.Write(Frame.Number);
        writer.Write(Checksum);
    }

    public void Deserialize(in BinaryBufferReader reader)
    {
        Frame = new(reader.ReadInt32());
        Checksum = reader.ReadInt32();
    }

    public readonly bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        using Utf8ObjectWriter writer = new(in utf8Destination, ref bytesWritten);
        return writer.Write(Frame) && writer.Write(Checksum);
    }
}
