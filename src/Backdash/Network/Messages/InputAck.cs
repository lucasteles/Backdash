using Backdash.Serialization;
using Backdash.Serialization.Internal;

namespace Backdash.Network.Messages;

[Serializable]
record struct InputAck : IUtf8SpanFormattable
{
    public Frame AckFrame;
    public readonly void Serialize(in BinarySpanWriter writer) => writer.Write(in AckFrame);

    public void Deserialize(in BinaryBufferReader reader) =>
        AckFrame = reader.ReadFrame();

    public readonly bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        using Utf8ObjectWriter writer = new(in utf8Destination, ref bytesWritten);
        return writer.Write(in AckFrame);
    }
}
