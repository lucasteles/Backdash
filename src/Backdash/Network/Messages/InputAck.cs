using Backdash.Data;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;
namespace Backdash.Network.Messages;
[Serializable]
record struct InputAck : IBinarySerializable, IUtf8SpanFormattable
{
    public Frame AckFrame;
    public readonly void Serialize(BinarySpanWriter writer) => writer.Write(in AckFrame.Number);
    public void Deserialize(BinarySpanReader reader) =>
        AckFrame = new(reader.ReadInt());
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
