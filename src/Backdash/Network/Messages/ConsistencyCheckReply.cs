using Backdash.Data;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[Serializable]
record struct ConsistencyCheckReply : IBinarySerializable, IUtf8SpanFormattable
{
    public Frame Frame;
    public int Checksum;

    public readonly void Serialize(BinarySpanWriter writer)
    {
        writer.Write(Frame.Number);
        writer.Write(Checksum);
    }

    public void Deserialize(BinarySpanReader reader)
    {
        Frame = new Frame(reader.ReadInt());
        Checksum = reader.ReadInt();
    }

    public readonly bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        using Utf8ObjectWriter writer = new(in utf8Destination, ref bytesWritten);
        if (!writer.Write(Frame)) return false;
        if (!writer.Write(Checksum)) return false;
        return true;
    }
}
