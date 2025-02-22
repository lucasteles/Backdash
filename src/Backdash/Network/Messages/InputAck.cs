using System.Runtime.InteropServices;
using Backdash.Data;
using Backdash.Serialization;
using Backdash.Serialization.Internal;

namespace Backdash.Network.Messages;

[Serializable, StructLayout(LayoutKind.Sequential)]
record struct InputAck : IUtf8SpanFormattable
{
    public Frame AckFrame;
    public readonly void Serialize(in BinaryRawBufferWriter writer) => writer.Write(in AckFrame.Number);

    public void Deserialize(in BinaryBufferReader reader) =>
        AckFrame = new(reader.ReadInt32());

    public readonly bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        using Utf8ObjectWriter writer = new(in utf8Destination, ref bytesWritten);
        return writer.Write(AckFrame);
    }
}
