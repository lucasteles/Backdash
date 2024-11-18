using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[Serializable, StructLayout(LayoutKind.Sequential)]
record struct QualityReply : IBinarySerializable, IUtf8SpanFormattable
{
    public long Pong;

    public readonly void Serialize(BinarySpanWriter writer) =>
        writer.Write(in Pong);

    public void Deserialize(BinarySpanReader reader) =>
        Pong = reader.ReadInt64();

    public readonly bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        bytesWritten = 0;
        using Utf8ObjectWriter writer = new(in utf8Destination, ref bytesWritten);
        return writer.Write(Pong);
    }
}
