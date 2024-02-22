using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[Serializable]
record struct SyncRequest : IBinarySerializable, IUtf8SpanFormattable
{
    public uint RandomRequest; /* please reply back with this random data */
    public long Ping;

    public readonly void Serialize(BinarySpanWriter writer)
    {
        writer.Write(in RandomRequest);
        writer.Write(in Ping);
    }

    public void Deserialize(BinarySpanReader reader)
    {
        RandomRequest = reader.ReadUInt();
        Ping = reader.ReadLong();
    }

    public readonly bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        using Utf8ObjectWriter writer = new(in utf8Destination, ref bytesWritten);
        if (!writer.Write(RandomRequest)) return false;
        if (!writer.Write(Ping)) return false;
        return true;
    }
}
