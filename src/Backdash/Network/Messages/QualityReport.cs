using Backdash.Serialization;
using Backdash.Serialization.Buffer;
namespace Backdash.Network.Messages;
[Serializable]
record struct QualityReport : IBinarySerializable, IUtf8SpanFormattable
{
    public int FrameAdvantage; /* what's the other guy's frame advantage? */
    public long Ping;
    public readonly void Serialize(BinarySpanWriter writer)
    {
        writer.Write(in FrameAdvantage);
        writer.Write(in Ping);
    }
    public void Deserialize(BinarySpanReader reader)
    {
        FrameAdvantage = reader.ReadInt();
        Ping = reader.ReadLong();
    }
    public readonly bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        bytesWritten = 0;
        using Utf8ObjectWriter writer = new(in utf8Destination, ref bytesWritten);
        if (!writer.Write(FrameAdvantage)) return false;
        if (!writer.Write(Ping)) return false;
        return true;
    }
}
