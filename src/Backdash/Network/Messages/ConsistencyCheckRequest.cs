using Backdash.Data;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[Serializable]
record struct ConsistencyCheckRequest : IBinarySerializable, IUtf8SpanFormattable
{
    public Frame Frame;

    public readonly void Serialize(BinarySpanWriter writer) =>
        writer.Write(Frame.Number);

    public void Deserialize(BinarySpanReader reader) =>
        Frame = new Frame(reader.ReadInt());

    public readonly bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        using Utf8ObjectWriter writer = new(in utf8Destination, ref bytesWritten);
        if (!writer.Write(Frame)) return false;
        return true;
    }
}
